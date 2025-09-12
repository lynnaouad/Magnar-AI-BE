using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.VectorData;

namespace Magnar.AI.Application.Models.Responses.VectorSearch;

public class VectorSearchResponse<T>
    where T : VectorStoreBase
{
    public bool Success { get; set; }

    public IEnumerable<VectorSearchResult<T>> SearchResults { get; set; } = [];

    public Error Error { get; set; } = new Error(Constants.Errors.ErrorOccured);
}
