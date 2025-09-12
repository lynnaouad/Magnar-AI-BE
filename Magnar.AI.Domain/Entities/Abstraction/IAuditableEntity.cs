namespace Magnar.AI.Domain.Entities.Abstraction;

public interface IAuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastModifiedAt { get; set; }

    public string CreatedBy { get; set; }

    public string? LastModifiedBy { get; set; }
}
