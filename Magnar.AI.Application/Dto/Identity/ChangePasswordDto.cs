namespace Magnar.AI.Application.Dto.Identity;

public sealed record ChangePasswordDto
{
    public string CurrentPassword { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
