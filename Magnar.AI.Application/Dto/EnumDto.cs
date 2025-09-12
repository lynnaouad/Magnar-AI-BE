namespace Magnar.AI.Application.Dto;

public sealed record EnumDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}