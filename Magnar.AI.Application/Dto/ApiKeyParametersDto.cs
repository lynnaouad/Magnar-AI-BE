namespace Magnar.AI.Application.Dto
{
    public class ApiKeyParametersDto
    {
        public string? Name { get; set; }

        public string? TenantId { get; set; }

        public string? Scopes { get; set; }

        public int? TtlMinutes { get; set; }

        public string? MetadataJson { get; set; }
    }
}
