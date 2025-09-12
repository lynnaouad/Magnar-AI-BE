namespace Magnar.AI.Application.Dto;

public sealed record ChangePasswordDto
{
    public int CompanyId { get; init; }

    public string CurrentPassword { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
