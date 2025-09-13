namespace Magnar.AI.Application.Dto;
public sealed record UpdateUserDto
{
    public ApplicationUserDto ApplicationUserDto { get; init; } = null!;
}
