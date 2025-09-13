namespace Magnar.AI.Application.Dto;

public sealed record ChangePasswordDto
{
    public string CurrentPassword { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
