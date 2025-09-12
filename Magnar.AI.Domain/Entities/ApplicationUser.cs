using Magnar.AI.Domain.Entities.Abstraction;
using Microsoft.AspNetCore.Identity;

namespace Magnar.AI.Domain.Entities;

public sealed class ApplicationUser : IdentityUser, IAuditableEntity
{
    public override string Email => base.Email ?? string.Empty;

    public override string UserName => base.UserName ?? string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = string.Empty;

    public bool Active { get; set; }

    public string? OldConfirmedEmail { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset? LastModifiedAt { get; set; }

    public string? LastModifiedBy { get; set; }
}
