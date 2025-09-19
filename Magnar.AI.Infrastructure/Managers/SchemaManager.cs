using DocumentFormat.OpenXml.Spreadsheet;
using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Application.Models;
using Serilog;
using System.Data.SqlClient;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Magnar.AI.Infrastructure.Managers
{
    public class SchemaManager : ISchemaManager
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;

        private readonly string baseFolder;

        private static readonly SemaphoreSlim _fileLock = new(1, 1);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = null, // keep PascalCase in JSON
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        #endregion

        #region Constrcutor
        public SchemaManager(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;

            baseFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/Annotations");

            // Ensure the folder exists
            if (!Directory.Exists(baseFolder))
            {
                Directory.CreateDirectory(baseFolder);
            }
        }
        #endregion

        public async Task<Result<IEnumerable<TableDto>>> LoadTablesFromDatabaseAsync(SqlServerProviderDetailsDto connection, CancellationToken cancellationToken = default)
        {
            // test connection
            var testSuccess = await unitOfWork.ProviderRepository.TestSqlProviderAsync(connection, cancellationToken);
            if (!testSuccess)
            {
                return Result<IEnumerable<TableDto>>.CreateFailure([new(Constants.Errors.ConnectionFailed)]);
            }

            // Build connection string
            var connectionString = unitOfWork.ProviderRepository.BuildSqlServerConnectionString(connection);

            var tables = new List<TableDto>();

            try
            {
                // Open SQL connection
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync(cancellationToken);

                string sql = @"
SELECT 
    t.TABLE_SCHEMA AS SchemaName,
    t.TABLE_NAME AS TableName,
    '[' + t.TABLE_SCHEMA + '].[' + t.TABLE_NAME + ']' AS FullName,
    '[' + c.COLUMN_NAME + ']' AS ColumnName,
    c.DATA_TYPE AS DataType,
    CASE WHEN c.IS_NULLABLE = 'YES' THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsNullable,
    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsPrimaryKey,
    CASE WHEN fk.parent_column_id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsForeignKey,
    ISNULL('[' + rs.name + '].['+ rt.name+ ']', '') AS ForeignKeyReferencedTable
FROM INFORMATION_SCHEMA.TABLES t
INNER JOIN INFORMATION_SCHEMA.COLUMNS c 
    ON t.TABLE_SCHEMA = c.TABLE_SCHEMA AND t.TABLE_NAME = c.TABLE_NAME
LEFT JOIN (
    SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
        ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
) pk
    ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA AND c.TABLE_NAME = pk.TABLE_NAME AND c.COLUMN_NAME = pk.COLUMN_NAME
-- Join to sys.foreign_key_columns to find foreign key columns
LEFT JOIN sys.tables t2
    ON t2.name = t.TABLE_NAME AND SCHEMA_NAME(t2.schema_id) = t.TABLE_SCHEMA
LEFT JOIN sys.columns c2
    ON c2.object_id = t2.object_id AND c2.name = c.COLUMN_NAME
LEFT JOIN sys.foreign_key_columns fk
    ON fk.parent_object_id = t2.object_id AND fk.parent_column_id = c2.column_id
-- Join to referenced table and schema
LEFT JOIN sys.tables rt
    ON rt.object_id = fk.referenced_object_id
LEFT JOIN sys.schemas rs
    ON rs.schema_id = rt.schema_id
WHERE t.TABLE_TYPE = 'BASE TABLE'
ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME, c.ORDINAL_POSITION;
                    ";

                await using var cmd = new SqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    var schemaName = reader.GetString(reader.GetOrdinal("SchemaName"));
                    var tableName = reader.GetString(reader.GetOrdinal("TableName"));
                    var fullName = reader.GetString(reader.GetOrdinal("FullName"));

                    var table = tables.FirstOrDefault(x => x.FullName == fullName);
                    if (table is null)
                    {
                        table = new TableDto
                        {
                            SchemaName = schemaName,
                            TableName = tableName,
                            FullName = fullName,
                            TableDescription = string.Empty,
                            Columns = []
                        };

                        tables.Add(table);
                    }
   
                    table.Columns.Add(new ColumnInfoDto
                    {
                        ColumnName = reader.GetString(reader.GetOrdinal("ColumnName")),
                        DataType = reader.GetString(reader.GetOrdinal("DataType")),
                        IsNullable = reader.GetBoolean(reader.GetOrdinal("IsNullable")),
                        IsPrimaryKey = reader.GetBoolean(reader.GetOrdinal("IsPrimaryKey")),
                        IsForeignKey = reader.GetBoolean(reader.GetOrdinal("IsForeignKey")),
                        ColumnDescription = string.Empty,
                        ForeignKeyReferencedTable = reader.GetString(reader.GetOrdinal("ForeignKeyReferencedTable")),
                    });
                }

                return Result<IEnumerable<TableDto>>.CreateSuccess(tables);

            }catch(Exception ex)
            {
                // Error occured while connecting to the server
                Log.Error(ex, ex.Message);
                return Result<IEnumerable<TableDto>>.CreateFailure([ new(ex.Message) ]);
            }                
        }

        public async Task<List<TableDto>> LoadFromFileAsync(int workspaceId, int providerId, CancellationToken cancellationToken = default)
        {
            var path = GetFilePath(workspaceId, providerId);

            if (!File.Exists(path))
            {
                return [];
            }

            await using var fs = File.OpenRead(path);
            try
            {
                var data = await JsonSerializer.DeserializeAsync<List<TableDto>>(fs, JsonOptions, cancellationToken);
                return data ?? [];
            }
            catch (Exception)
            {
                return [];
            }
        }

        public async Task UpsertFileAsync(IEnumerable<TableDto> incoming, int workspaceId, int providerId, CancellationToken cancellationToken = default)
        {
            var path = GetFilePath(workspaceId, providerId);

            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                var existing = await LoadFromFileAsync(workspaceId, providerId, cancellationToken);

                // Map existing by key
                var map = new Dictionary<string, TableDto>(StringComparer.OrdinalIgnoreCase);
                foreach (var t in existing)
                {
                    map[Key(t)] = t;
                }
                    
                // Build a set of incoming keys (what the file should end up containing)
                var incomingList = incoming?.ToList() ?? [];
                var incomingKeys = new HashSet<string>( incomingList.Select(Key), StringComparer.OrdinalIgnoreCase);

                // Upsert/merge each incoming table
                foreach (var inc in incomingList)
                {
                    var k = Key(inc);

                    if (!map.TryGetValue(k, out var ex))
                    {
                        inc.FullName = k;
                        inc.Columns ??= [];
                        map[k] = inc;
                        continue;
                    }

                    // --- merge into existing ---
                    ex.SchemaName = inc.SchemaName;
                    ex.TableName = inc.TableName;
                    ex.FullName = k;
                    ex.TableDescription = inc.TableDescription;
                    ex.IsSelected = inc.IsSelected; // keep selection flag

                    var exCols = ex.Columns?.ToDictionary(c => c.ColumnName, StringComparer.OrdinalIgnoreCase)
                               ?? new Dictionary<string, ColumnInfoDto>(StringComparer.OrdinalIgnoreCase);

                    foreach (var c in inc.Columns ?? Enumerable.Empty<ColumnInfoDto>())
                    {
                        exCols[c.ColumnName] = new ColumnInfoDto
                        {
                            ColumnName = c.ColumnName,
                            DataType = c.DataType,
                            IsNullable = c.IsNullable,
                            IsPrimaryKey = c.IsPrimaryKey,
                            IsForeignKey = c.IsForeignKey,
                            ColumnDescription = c.ColumnDescription
                        };
                    }

                    // prune columns missing from incoming
                    var incomingNames = new HashSet<string>(
                        (inc.Columns ?? Enumerable.Empty<ColumnInfoDto>()).Select(c => c.ColumnName),
                        StringComparer.OrdinalIgnoreCase);

                    foreach (var name in exCols.Keys.Except(incomingNames).ToList())
                    {
                        exCols.Remove(name);
                    }
                       
                    ex.Columns = [.. exCols.Values.OrderBy(c => c.ColumnName, StringComparer.OrdinalIgnoreCase)];

                    map[k] = ex;
                }

                // remove tables that are not in incoming (unselected/dropped)
                foreach (var staleKey in map.Keys.Except(incomingKeys, StringComparer.OrdinalIgnoreCase).ToList())
                    map.Remove(staleKey);

                var result = map.Values
                                .OrderBy(t => t.SchemaName, StringComparer.OrdinalIgnoreCase)
                                .ThenBy(t => t.TableName, StringComparer.OrdinalIgnoreCase)
                                .ToList();

                await SaveAsync(result, path, cancellationToken);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task SaveAsync(IEnumerable<TableDto> tables, string filePath, CancellationToken cancellationToken = default)
        {
            var dir = Path.GetDirectoryName(filePath)!;
            var fileName = Path.GetFileName(filePath);
            var tmp = Path.Combine(dir, $"{fileName}.{Guid.NewGuid():N}.tmp");
            var bak = Path.Combine(dir, $"{fileName}.bak");

            try
            {
                await using (var fs = new FileStream(
                    tmp,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    64 * 1024,
                    FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await JsonSerializer.SerializeAsync(fs, tables, JsonOptions, cancellationToken);
                    await fs.FlushAsync(cancellationToken);
                    fs.Flush(true); // flush to disk
                }

                if (File.Exists(filePath))
                {
                    File.Replace(tmp, filePath, bak); // atomic replace + backup
                }
                else
                {
                    File.Move(tmp, filePath);
                }                  
            }
            catch
            {
                if (File.Exists(tmp)) File.Delete(tmp);
                throw;
            } 
        }

        public async Task<int> RemoveMissingTablesAsync(int workspaceId, int providerId, CancellationToken cancellationToken = default)
        {
            var provider =  await unitOfWork.ProviderRepository.GetProviderAsync(providerId, cancellationToken);
            if(provider is null || provider.Type != Domain.Static.ProviderTypes.SqlServer || provider.Details?.SqlServerConfiguration is null)
            {
                return 0;
            }

            var result = await LoadTablesFromDatabaseAsync(provider.Details.SqlServerConfiguration, cancellationToken);
            if(!result.Success)
            {
                return 0;
            }

            var live = result.Value;
            var fileTables = await LoadFromFileAsync(workspaceId, providerId, cancellationToken);

            // mark which to keep
            var keep = new List<TableDto>(fileTables.Count);
            var removed = new List<TableDto>();

            foreach (var t in fileTables)
            {
                if (live.Select(x => x.FullName).Contains(t.FullName))
                {
                    keep.Add(t);
                }
                else
                {
                    removed.Add(t);
                }
            }

            if (removed.Count == 0)
            {
                return 0;
            }
                
            // save cleaned file (sorted)
            keep = [.. keep
                .OrderBy(x => x.SchemaName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.TableName, StringComparer.OrdinalIgnoreCase)];

            var path = GetFilePath(workspaceId, providerId);

            await _fileLock.WaitAsync(cancellationToken);

            try
            {
                await SaveAsync(keep, path, cancellationToken);
            }
            finally
            {
                _fileLock.Release();
            }

            return removed.Count;
        }

        public async Task<IEnumerable<TableDto>> MergeSelectionsFromFileAsync(int workspaceId, int providerId, CancellationToken cancellationToken = default)
        {
            var path = GetFilePath(workspaceId, providerId);

            var provider = await unitOfWork.ProviderRepository.GetProviderAsync(providerId, cancellationToken);
            if (provider is null || provider.Type != Domain.Static.ProviderTypes.SqlServer || provider.Details?.SqlServerConfiguration is null)
            {
                return [];
            }

            var result = await LoadTablesFromDatabaseAsync(provider.Details.SqlServerConfiguration, cancellationToken);
            if (!result.Success)
            {
                return [];
            }

            var liveTables = result.Value;

            static string SchemaMergeKey(TableDto t) => t.FullName;

            var saved = await LoadFromFileAsync(workspaceId, providerId, cancellationToken);
            var savedMap = saved.ToDictionary(SchemaMergeKey, StringComparer.OrdinalIgnoreCase);

            foreach (var t in liveTables)
            {
                var key = t.FullName;
                if (savedMap.TryGetValue(key, out var s))
                {
                    // Selection from file
                    t.IsSelected = true;
                    t.TableDescription = s.TableDescription;

                    // Column descriptions (optional)
                    if ((t.Columns?.Count > 0) && (s.Columns?.Count > 0))
                    {
                        var sCols = s.Columns.ToDictionary(c => c.ColumnName, StringComparer.OrdinalIgnoreCase);
                        foreach (var c in t.Columns)
                        {
                            if (sCols.TryGetValue(c.ColumnName, out var sc))
                            {
                                c.ColumnDescription = sc.ColumnDescription;
                            }
                        }
                    }
                }
                else
                {
                    // Not present in file -> not selected
                    t.IsSelected = false;
                }
            }

            return liveTables;
        }

        #region Private Methods
        private static string Key(TableDto t) => string.IsNullOrWhiteSpace(t.FullName) ? $"[{t.SchemaName}].[{t.TableName}]" : t.FullName;

        private string GetFilePath(int workspaceId, int connectionId) => Path.Combine(baseFolder, $"annotations_{workspaceId}_{connectionId}.json");
        #endregion
    }
}
