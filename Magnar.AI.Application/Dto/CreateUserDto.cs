namespace Magnar.AI.Application.Dto;

public sealed record CreateUserDto
{
    public int CompanyId { get; init; } = 0;

    public ApplicationUserDto ApplicationUserDto { get; init; } = null!;
}
