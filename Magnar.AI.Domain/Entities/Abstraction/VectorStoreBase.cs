using Microsoft.Extensions.VectorData;

namespace Magnar.AI.Entities.Abstraction;

public abstract class VectorStoreBase
{
    [VectorStoreKey]
    public Guid ID { get; set; }

    [VectorStoreData]
    public string Text { get; set; } = string.Empty;

    [VectorStoreVector(Dimensions: 1536, DistanceFunction = DistanceFunction.CosineDistance)]
    public float[] Embedding { get; set; } = [];

    [VectorStoreData]
    public int ChunckIndex { get; set; }
}
