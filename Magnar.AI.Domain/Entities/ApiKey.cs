using Magnar.AI.Domain.Entities.Abstraction;

namespace Magnar.AI.Domain.Entities;

public class ApiKey : EntityBase
{
    public string PublicId { get; set; } = string.Empty;
    
    public string Hash { get; set; } = string.Empty;
   
    public int OwnerUserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public string TenantId { get; set; } = string.Empty;
    
    public string ScopesCsv { get; set; } = string.Empty;

    public string? Name { get; set; }
   
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresUtc { get; set; }

    public DateTime? RevokedUtc { get; set; }

    public DateTime? LastUsedUtc { get; set; }

    public string? MetadataJson { get; set; }

    public bool IsActive(DateTime now) => RevokedUtc is null && (ExpiresUtc is null || ExpiresUtc > now);

    public IEnumerable<string> GetScopes() => (ScopesCsv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}