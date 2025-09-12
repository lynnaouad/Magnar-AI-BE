namespace Magnar.AI.Application.Dto;

public class ChunkDto
{
    public int Index { get; set; }

    public string Text { get; set; } = string.Empty;

    public float[] Embedding { get; set; } = [];
}
