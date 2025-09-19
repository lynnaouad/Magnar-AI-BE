using Magnar.AI.Domain.Entities.Abstraction;
using Magnar.AI.Domain.Static;

namespace Magnar.AI.Domain.Entities
{
    public class ApiProviderDetails : EntityBase, IAuditableEntity
    {
        public int ProviderId { get; set; }

        public Provider Provider { get; set; } = null!;

        public string PluginName { get; set; } = string.Empty;

        public string FunctionName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string ApiUrl { get; set; } = string.Empty;

        public HttpMethods HttpMethod { get; set; }

        public string? Payload { get; set; }

        public string? ParametersJson { get; set; }   

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? LastModifiedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public string? LastModifiedBy { get; set; }
    }
}
