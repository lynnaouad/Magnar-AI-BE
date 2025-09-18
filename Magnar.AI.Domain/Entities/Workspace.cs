using Magnar.AI.Domain.Entities.Abstraction;

namespace Magnar.AI.Domain.Entities
{
    public class Workspace : EntityBase, IAuditableEntity
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
       
        public DateTimeOffset? LastModifiedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
       
        public string? LastModifiedBy { get; set; }
    }
}
