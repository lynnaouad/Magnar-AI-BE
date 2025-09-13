namespace Magnar.AI.Application.Dto.Identity;

public sealed record CreateUserDto
{
    public ApplicationUserDto ApplicationUserDto { get; init; } = null!;
}
