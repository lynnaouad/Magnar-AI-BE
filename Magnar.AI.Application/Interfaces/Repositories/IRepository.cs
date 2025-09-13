using System.Linq.Expressions;
using Magnar.AI.Application.Models.Responses;
using Magnar.AI.Domain.Entities.Abstraction;
using Microsoft.AspNetCore.OData.Query;

namespace Magnar.AI.Application.Interfaces.Repositories;

public interface IRepository<TEntity>
    where TEntity : EntityBase
{
    /// <summary>
    /// Retrieves an entity by its unique identifier (primary key).
    /// </summary>
    /// <remarks>
    /// This method is typically used to fetch a single entity from the database based on its primary key.
    /// If no entity is found with the specified <paramref name="id"/>, the method returns <c>null</c>.
    /// </remarks>
    /// <typeparam name="TEntity">The type of the entity being retrieved.</typeparam>
    /// <param name="id">The unique identifier of the entity to retrieve.</param>
    /// <param name="tracking"> This ensures that the query results are tracked by the context.</param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// The task result contains the entity if found; otherwise, <c>null</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="id"/> parameter is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if multiple entities with the same identifier are found, which violates uniqueness.
    /// </exception>
    Task<TEntity> GetAsync(int id, bool tracking = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities asynchronously.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being retrieved.</typeparam>
    /// <param name="tracking"> This ensures that the query results are tracked by the context.</param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// The task result contains a read-only list of entities.
    /// </returns>
    Task<IReadOnlyList<TEntity>> GetAsync(bool tracking = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a read-only list of entities that match the specified filter condition asynchronously.
    /// </summary>
    /// <param name="filter">A function to test each entity for a condition. Only entities that satisfy the condition are returned.</param>
    /// <param name="tracking"> This ensures that the query results are tracked by the context.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete. Defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a read-only list of entities
    /// that match the specified filter condition. If no entities match the condition, an empty list is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="filter"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
    Task<IReadOnlyList<TEntity>> WhereAsync(Expression<Func<TEntity, bool>> filter, bool tracking = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously returns the first element of a sequence that satisfies a specified condition,
    /// or a default value if no such element is found.
    /// </summary>
    /// <param name="filter">A function to test each element for a condition.</param>
    /// <param name="tracking">Indicates whether the entity should be tracked by the context.
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// If set to <c>false</c>, the entity will be returned as a detached entity.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the first element
    /// that satisfies the condition, or <c>null</c> if no such element is found.
    /// </returns>
    Task<TEntity> FirstOrDeafultAsync(Expression<Func<TEntity, bool>> filter, bool tracking = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new entity asynchronously.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity to create.</typeparam>
    /// <param name="entity">The entity to add to the database.</param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation that contains the created entity.</returns>
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple new entities asynchronously.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entities to create.</typeparam>
    /// <param name="entities">The collection of entities to add to the database.</param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task CreateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity in the database.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being updated.</typeparam>
    /// <param name="entity">The entity to update.</param>
    void Update(TEntity entity);

    /// <summary>
    /// Updates multiple existing entities in the database.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entities being updated.</typeparam>
    /// <param name="entities">The collection of entities to update.</param>
    void Update(IEnumerable<TEntity> entities);

    /// <summary>
    /// Deletes an entity by its unique identifier.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being deleted.</typeparam>
    /// <param name="entityId">The unique identifier of the entity to delete.</param>
    void Delete(int entityId);

    /// <summary>
    /// Deletes multiple entities by their unique identifiers.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entities being deleted.</typeparam>
    /// <param name="keys">The collection of unique identifiers of the entities to delete.</param>
    void Delete(IEnumerable<int> keys);

    /// <summary>
    /// Deletes a specific entity.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being deleted.</typeparam>
    /// <param name="entity">The entity to delete.</param>
    void Delete(TEntity entity);

    /// <summary>
    /// Deletes multiple specific entities.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entities being deleted.</typeparam>
    /// <param name="entities">The collection of entities to delete.</param>
    public void Delete(IEnumerable<TEntity> entities);

    /// <summary>
    /// This method can be used to access data from an SQL view, entity... without needing to define a specific repository method for each, promoting code reusability and flexibility.
    /// </summary>
    /// <returns>It returns typeof.<IQueryable<T>> that contains all the data.</returns>
    public IQueryable<T> GetAsQueryable<T>()
        where T : class;

    /// <summary>
    /// This method can be used to access data and filter the result based on provided filter query.
    /// </summary>
    /// <returns> It returns typeof.<OdataResponse> that contains the filtered list and the total count.<T>.</returns>
    public Task<OdataResponse<T>> OdataGetAsync<T>(ODataQueryOptions<T> filterOptions = null, Expression<Func<T, bool>> requiredFilters = null, CancellationToken cancellationToken = default)
        where T : class;
}
