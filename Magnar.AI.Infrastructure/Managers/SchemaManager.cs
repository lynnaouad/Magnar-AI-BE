using AutoMapper;
using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Application.Models;
using Magnar.AI.Domain.Static;
using Serilog;
using System.Data.SqlClient;

namespace Magnar.AI.Infrastructure.Managers
{
    public class SchemaManager : ISchemaManager
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        #endregion

        #region Constrcutor
        public SchemaManager(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }
        #endregion

        public async Task<Result<IEnumerable<TableDto>>> GetTablesAsync(CancellationToken cancellationToken = default)
        {
            // Get default connection
            var sqlConnection = await unitOfWork.ProviderRepository.FirstOrDefaultAsync(x => x.Type == ProviderTypes.SqlServer, false, cancellationToken);
            if (sqlConnection is null)
            {
                return Result<IEnumerable<TableDto>>.CreateFailure([new(Constants.Errors.NoDefaultConnectionConfigured)]);
            }

            var defaultConnection = mapper.Map<ProviderDto>(sqlConnection);

            // test default connection
            var testSuccess = await unitOfWork.ProviderRepository.TestSqlProviderAsync(defaultConnection.Details.SqlServerConfiguration, cancellationToken);
            if (!testSuccess)
            {
                return Result<IEnumerable<TableDto>>.CreateFailure([new(Constants.Errors.ConnectionFailed)]);
            }

            // Build connection string
            var connectionString = unitOfWork.ProviderRepository.BuildSqlServerConnectionString(defaultConnection.Details.SqlServerConfiguration);

            var list = new List<TableDto>();

            try
            {
                // Open SQL connection
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync(cancellationToken);

                // SQL query to fetch user-defined tables with their schema names
                string sql = @"
                    SELECT s.name AS SchemaName, t.name AS TableName
                    FROM sys.tables t
                    JOIN sys.schemas s ON t.schema_id = s.schema_id
                    WHERE t.is_ms_shipped = 0
                    ORDER BY s.name, t.name";

                await using var cmd = new SqlCommand(sql, conn);
                await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);

                // Loop through the result set and map to TableDto objects
                while (await rdr.ReadAsync(cancellationToken))
                {
                    list.Add(new()
                    {
                        SchemaName = rdr.GetString(0),
                        TableName = rdr.GetString(1)
                    });
                }

                return Result<IEnumerable<TableDto>>.CreateSuccess(list);

            }catch(Exception ex)
            {
                // Error occured while connecting to the server
                Log.Error(ex, ex.Message);
                return Result<IEnumerable<TableDto>>.CreateFailure([ new(ex.Message) ]);
            }                
        }

        public async Task<Result<TableInfoDto>> GetTableInfoAsync(string schema, string table, CancellationToken cancellationToken = default)
        {
            // Get default connection
            var sqlConnection = await unitOfWork.ProviderRepository.FirstOrDefaultAsync(x=> x.Type == ProviderTypes.SqlServer, false, cancellationToken);
            if (sqlConnection is null)
            {
                return Result<TableInfoDto>.CreateFailure([new(Constants.Errors.NoDefaultConnectionConfigured)]);
            }

            var defaultConnection = mapper.Map<ProviderDto>(sqlConnection);

            // test default connection
            var testSuccess = await unitOfWork.ProviderRepository.TestSqlProviderAsync(defaultConnection.Details.SqlServerConfiguration, cancellationToken);

            if (!testSuccess)
            {
                return Result<TableInfoDto>.CreateFailure([new(Constants.Errors.ConnectionFailed)]);
            }

            // Build connection string
            var connectionString = unitOfWork.ProviderRepository.BuildSqlServerConnectionString(defaultConnection.Details.SqlServerConfiguration);

            // Open SQL connection
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);

            var columns = new List<ColumnInfoDto>();

            // Retrieve columns + PK
            var colSql = @"
                SELECT c.name AS ColumnName,
                       t.name AS DataType,
                       c.is_nullable,
                       CASE WHEN pkCol.column_id IS NULL THEN 0 ELSE 1 END AS IsPK
                FROM sys.columns c
                JOIN sys.types t ON c.user_type_id = t.user_type_id
                JOIN sys.tables tb ON c.object_id = tb.object_id
                JOIN sys.schemas s ON tb.schema_id = s.schema_id
                LEFT JOIN (
                    SELECT ic.object_id, ic.column_id
                    FROM sys.indexes i
                    JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                    WHERE i.is_primary_key = 1
                ) pkCol ON pkCol.object_id = c.object_id AND pkCol.column_id = c.column_id
                WHERE s.name = @schema AND tb.name = @table
                ORDER BY c.column_id";

            await using (var colCmd = new SqlCommand(colSql, conn))
            {
                colCmd.Parameters.AddWithValue("@schema", schema);
                colCmd.Parameters.AddWithValue("@table", table);
                
                await using var rdr = await colCmd.ExecuteReaderAsync(cancellationToken);
                while (await rdr.ReadAsync(cancellationToken))
                {
                    columns.Add(new ColumnInfoDto()
                    {
                        ColumnName = rdr.GetString(0),
                        DataType = rdr.GetString(1),
                        IsNullable = rdr.GetBoolean(2),
                        IsPrimaryKey = rdr.GetInt32(3) == 1,
                    });
                }
            }

            // Retrieve foreign keys
            var fks = new List<ForeignKeyInfoDto>();

            // SQL query for foreign key info:
            // - Column name, referenced schema, referenced table, referenced column
            var fkSql = @"
                SELECT COL_NAME(fk.parent_object_id, fkc.parent_column_id) AS ColumnName,
                       rs.name AS RefSchema, rt.name AS RefTable,
                       COL_NAME(rt.object_id, fkc.referenced_column_id) AS RefColumn
                FROM sys.foreign_keys fk
                JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                JOIN sys.tables pt ON fk.parent_object_id = pt.object_id
                JOIN sys.schemas ps ON pt.schema_id = ps.schema_id
                JOIN sys.tables rt ON fk.referenced_object_id = rt.object_id
                JOIN sys.schemas rs ON rt.schema_id = rs.schema_id
                WHERE ps.name = @schema AND pt.name = @table
                ORDER BY ColumnName";

            // Execute query and read results
            await using (var fkCmd = new SqlCommand(fkSql, conn))
            {
                fkCmd.Parameters.AddWithValue("@schema", schema);
                fkCmd.Parameters.AddWithValue("@table", table);
               
                await using var rdr = await fkCmd.ExecuteReaderAsync(cancellationToken);
                while (await rdr.ReadAsync(cancellationToken))
                {
                    fks.Add(new ForeignKeyInfoDto()
                    {
                        ColumnName = rdr.GetString(0),
                        ReferencedSchema = rdr.GetString(1),
                        ReferencedTable = rdr.GetString(2),
                        ReferencedColumn = rdr.GetString(3)
                    });
                }
            }

            var tableInfo = new TableInfoDto()
            {
                SchemaName = schema,
                TableName = table,
                Columns = columns,
                ForeignKeys = fks
            };

            return Result<TableInfoDto>.CreateSuccess(tableInfo);
        }
    }
}
