using Magnar.AI.Entities.Abstraction;
using Microsoft.Extensions.VectorData;

namespace Magnar.AI.Domain.Entities.Vectors;

public class DatabaseSchemaEmbedding : VectorStoreBase
{
    [VectorStoreData]
    public string Name { get; set; } = string.Empty;

    [VectorStoreData]
    public int ConnectionId { get; set; }
}
