namespace Magnar.AI.Application.Models;

public sealed record UserGrant
{
    public int Id { get; init; }

    public string Key { get; init; } = default!;

    public string Type { get; init; } = default!;

    public string SubjectId { get; init; } = default!;

    public string ClientId { get; init; } = default!;

    public DateTime CreationTime { get; init; }

    public DateTime? Expiration { get; init; }

    public string Data { get; init; } = default!;
}
