namespace Magnar.AI.Application.Dto.AI;

public class ChunkDto
{
    public int Index { get; set; }

    public string Text { get; set; } = string.Empty;

    public float[] Embedding { get; set; } = [];
}
