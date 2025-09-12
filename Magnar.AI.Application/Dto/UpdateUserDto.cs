namespace Magnar.AI.Application.Dto;
public sealed record UpdateUserDto
{
    public int CompanyId { get; init; }

    public ApplicationUserDto ApplicationUserDto { get; init; } = null!;
}
