namespace Magnar.AI.Application.Dto.ApiKeys
{
    public class ApiKeyDto
    {
        public int Id { get; set; }
         
        public string PublicId { get; set; } = string.Empty;

        public string? Name { get; set; }

        public string TenantId { get; set; } = string.Empty;

        public IEnumerable<string> Scopes { get; set; } = [];

        public DateTime CreatedUtc { get; set; }

        public DateTime? ExpiresUtc { get; set; }

        public DateTime? RevokedUtc { get; set; }

        public DateTime? LastUsedUtc { get; set; }
    }

    public class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<ApiKey, ApiKeyDto>()
                 .ForMember(dest => dest.Scopes, opt => opt.MapFrom(src => src.GetScopes()));
        }
    }
}