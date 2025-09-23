using Magnar.AI.Application.Helpers;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Data.SqlClient;

namespace Magnar.AI.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly MagnarAIDbContext dbContext;
    private readonly IServiceProvider serviceProvider;
    private bool disposed = false;

    private IDbContextTransaction transaction = null;

    public UnitOfWork(MagnarAIDbContext context, IServiceProvider serviceProvider)
    {
        dbContext = context;
        this.serviceProvider = serviceProvider;
    }

    public IRepository<Workspace> WorkspaceRepository => serviceProvider.GetRequiredService<IRepository<Workspace>>();

    public IIdentityRepository IdentityRepository => serviceProvider.GetRequiredService<IIdentityRepository>();

    public IProviderRepository ProviderRepository => serviceProvider.GetRequiredService<IProviderRepository>();
    
    public IApiKeyRepository ApiKeyRepository => serviceProvider.GetRequiredService<IApiKeyRepository>();

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int result = await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (transaction is not null)
        {
            throw new InvalidOperationException(Constants.ExceptionMessages.TransactionAlreadyCreated);
        }

        transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (transaction is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (transaction is not null)
        {
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
            }
        }
    }

    public async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(string sqlQuery, string connectionString, CancellationToken cancellationToken = default)
    {
        if (!Utilities.IsSafeSelectQuery(sqlQuery))
        {
            throw new InvalidOperationException("Only SELECT queries are allowed");
        }

        var rows = new List<Dictionary<string, object>>();

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new SqlCommand(sqlQuery, conn)
        {
            CommandType = CommandType.Text,
            CommandTimeout = 30
        };

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            rows.Add(row);
        }

        return rows;
    }

    public void Dispose()
    {
        if (!disposed)
        {
            transaction?.Dispose();

            dbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!disposed)
        {
            transaction?.Dispose();

            await dbContext.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        disposed = true;
    }

    ~UnitOfWork()
    {
        disposed = true;
    }
}
