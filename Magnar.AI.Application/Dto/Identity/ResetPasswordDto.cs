namespace Magnar.AI.Application.Dto.Identity;

public sealed record ResetPasswordDto
{
    public string ResetToken { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
