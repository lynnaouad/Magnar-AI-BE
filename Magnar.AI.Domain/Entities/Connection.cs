using Magnar.AI.Domain.Entities.Abstraction;
using Magnar.AI.Domain.Static;

namespace Magnar.AI.Domain.Entities
{
    public class Connection : EntityBase, IAuditableEntity
    {
        public ProviderTypes Provider { get; set; }

        public bool IsDefault { get; set; }

        public string? Details { get; set; }
       
        public DateTimeOffset CreatedAt { get; set; }
       
        public DateTimeOffset? LastModifiedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
        
        public string? LastModifiedBy { get; set; }
    }
}
