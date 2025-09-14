using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Features.Connection.Queries;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Application.Models;
using MediatR;
using System.Data.SqlClient;

namespace Magnar.AI.Infrastructure.Managers
{
    public class SchemaManager : ISchemaManager
    {
       private readonly IMediator mediator;

       public SchemaManager(IMediator mediator)
       {
            this.mediator = mediator;
       }

        public async Task<Result<IEnumerable<TableDto>>> GetTablesAsync(CancellationToken cancellationToken = default)
        {
            var result = await mediator.Send(new GetDefaultSqlServerConnectionStringQuery(), cancellationToken);
            if (!result.Success)
            {
                return Result<IEnumerable<TableDto>>.CreateFailure(result.Errors);
            }

            var connectionString = result.Value;

            var list = new List<TableDto>();
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);

            string sql = @"
            SELECT s.name AS SchemaName, t.name AS TableName
            FROM sys.tables t
            JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE t.is_ms_shipped = 0
            ORDER BY s.name, t.name";

            await using var cmd = new SqlCommand(sql, conn);
            await using var rdr = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await rdr.ReadAsync(cancellationToken))
            {
                list.Add(new()
                {
                    SchemaName = rdr.GetString(0),
                    TableName = rdr.GetString(1)
                });
            }

            return Result<IEnumerable<TableDto>>.CreateSuccess(list);
        }

        public async Task<Result<TableInfoDto>> GetTableInfoAsync(string schema, string table, CancellationToken cancellationToken = default)
        {
            var result = await mediator.Send(new GetDefaultSqlServerConnectionStringQuery(), cancellationToken);
            if (!result.Success)
            {
                return Result<TableInfoDto>.CreateFailure(result.Errors);
            }

            var connectionString = result.Value;

            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);

            // Columns + PK info
            var columns = new List<ColumnInfoDto>();
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
                await using var rdr = await colCmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
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

            // FKs
            var fks = new List<ForeignKeyInfoDto>();
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

            await using (var fkCmd = new SqlCommand(fkSql, conn))
            {
                fkCmd.Parameters.AddWithValue("@schema", schema);
                fkCmd.Parameters.AddWithValue("@table", table);
                await using var rdr = await fkCmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
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
