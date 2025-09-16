using Magnar.AI.Application.Models.Responses.VectorSearch;
using Microsoft.Extensions.VectorData;

namespace Magnar.AI.Application.Interfaces.Managers;

public interface IVectorStoreManager<T> where T : VectorStoreBase
{
    /// <summary>
    /// Uploads a collection of embeddings to the vector store.
    /// </summary>
    /// <param name="list">The collection of entities to upload.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InsertAsync(IEnumerable<T> list, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes embeddings from vector store.
    /// </summary>
    /// <param name="filters">Filter query parameters.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task DeleteAsync(Dictionary<string, object> filters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entire collection.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    Task DeleteTableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a vector search using the provided prompt and search options.
    /// </summary>
    /// <param name="prompt">The search prompt.</param>
    /// <param name="top">The number of top results to return.</param>
    /// <param name="searchOptions">The options to use for the vector search.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns> <see cref="VectorSearchResponse"/> containing a collection of <see cref="VectorSearchResult{T}"/> containing the search results.</returns>
    Task<VectorSearchResponse<T>> VectorSearchAsync(string prompt, int top, VectorSearchOptions<T> searchOptions, CancellationToken cancellationToken = default);

    Task<IEnumerable<string>> ListIdsAsync(Dictionary<string, object> filters, CancellationToken cancellationToken = default);
}
