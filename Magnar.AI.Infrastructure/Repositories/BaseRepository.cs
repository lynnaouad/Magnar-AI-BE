using Magnar.AI.Application.Models.Responses;
using Magnar.AI.Domain.Entities.Abstraction;
using Magnar.AI.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.OData.Query;
using Serilog;
using System.Linq.Expressions;

namespace Magnar.AI.Infrastructure.Repositories;

public class BaseRepository<TEntity> : IRepository<TEntity>
    where TEntity : EntityBase, new()
{
    protected MagnarAIDbContext Context { get; }

    public BaseRepository(MagnarAIDbContext context)
    {
        Context = context;
        _dbSet = Context.Set<TEntity>();
    }

    private readonly DbSet<TEntity> _dbSet;

    public async Task<TEntity> GetAsync(int id, bool tracking = true, CancellationToken cancellationToken = default)
    {
        return tracking ? await _dbSet.FindAsync([id], cancellationToken: cancellationToken)
                        : await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> GetAsync(bool tracking = true, CancellationToken cancellationToken = default)
    {
        return tracking ? await _dbSet.ToListAsync(cancellationToken)
                        : await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> WhereAsync(Expression<Func<TEntity, bool>> filter, bool tracking = true, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> values = _dbSet.Where(filter);
        return tracking ? await values.ToListAsync(cancellationToken)
                        : await values.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> filter, bool tracking = true, CancellationToken cancellationToken = default)
    {
        return tracking ? await _dbSet.FirstOrDefaultAsync(filter, cancellationToken)
                        : await _dbSet.AsNoTracking().FirstOrDefaultAsync(filter, cancellationToken);
    }

    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task CreateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public void Update(IEnumerable<TEntity> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    public void Delete(int entityId)
    {
        TEntity entity = _dbSet.Find(entityId);
        if (entity != default)
        {
            Delete(new TEntity { Id = entityId });
        }
    }

    public void Delete(IEnumerable<int> keys)
    {
        IQueryable<TEntity> entities = _dbSet.Where(x => keys.Contains(x.Id));

        Delete(entities);
    }

    public void Delete(TEntity entity)
    {
        TEntity existingEntity = _dbSet.Find(entity.Id);
        if (existingEntity != default)
        {
            _ = _dbSet.Remove(existingEntity);
        }
    }

    public void Delete(IEnumerable<TEntity> entities)
    {
        IQueryable<TEntity> existingEntities = _dbSet.Where(x => entities.Select(x => x.Id).Contains(x.Id));
        _dbSet.RemoveRange(existingEntities);
    }

    public IQueryable<T> GetAsQueryable<T>()
        where T : class
    {
        return Context.Set<T>().AsQueryable();
    }

    public async Task<OdataResponse<T>> OdataGetAsync<T>(ODataQueryOptions<T> filterOptions = null, Expression<Func<T, bool>> requiredFilters = null, CancellationToken cancellationToken = default)
        where T : class
    {
        OdataResponse<T> response = new()
        { Value = [], TotalCount = 0 };

        try
        {
            IQueryable<T> viewData = GetAsQueryable<T>();
            if (viewData is null)
            {
                return response;
            }

            if (requiredFilters is not null)
            {
                viewData = viewData.Where(requiredFilters);
            }

            if (filterOptions?.Filter is not null)
            {
                viewData = (IQueryable<T>)filterOptions.Filter.ApplyTo(viewData, new ODataQuerySettings());
            }

            int totalCount = await viewData.CountAsync(cancellationToken);

            if (filterOptions is null)
            {
                response.Value = await viewData.AsNoTracking().ToListAsync(cancellationToken);
                response.TotalCount = totalCount;

                return response;
            }

            if (filterOptions.ApplyTo(viewData, new ODataQuerySettings()) is not IQueryable<T> listQueryable)
            {
                return response;
            }

            if (listQueryable is null || !listQueryable.Any())
            {
                return response;
            }

            List<T> values = await listQueryable.AsNoTracking().ToListAsync(cancellationToken);
            if (values is null || values.Count == 0)
            {
                return response;
            }

            response.Value = values;
            response.TotalCount = totalCount;

            return response;
        }
        catch (Exception ex)
        {
            Log.Error(ex, ex.Message);
            return response;
        }
    }
}
