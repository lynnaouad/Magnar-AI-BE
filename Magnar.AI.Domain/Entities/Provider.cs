using Magnar.AI.Domain.Entities.Abstraction;
using Magnar.AI.Domain.Static;

namespace Magnar.AI.Domain.Entities
{
    public class Provider : EntityBase, IAuditableEntity
    {
        public int WorkspaceId { get; set; }

        public Workspace Workspace { get; set; } = null!;

        public bool IsDefault { get; set; }

        public string Name { get; set; } = string.Empty;

        public ProviderTypes Type { get; set; }

        public string? Details { get; set; }
       
        public DateTimeOffset CreatedAt { get; set; }
       
        public DateTimeOffset? LastModifiedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
        
        public string? LastModifiedBy { get; set; }

        public ICollection<ApiProviderDetails> ApiProviderDetails { get; set; } = [];
    }
}
