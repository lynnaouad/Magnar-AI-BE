namespace Magnar.AI.Application.Dto;

public sealed record CreateUserDto
{
    public ApplicationUserDto ApplicationUserDto { get; init; } = null!;
}
