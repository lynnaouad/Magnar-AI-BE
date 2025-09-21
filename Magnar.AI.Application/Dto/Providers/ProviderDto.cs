using Newtonsoft.Json;

namespace Magnar.AI.Application.Dto.Providers
{
    public class ProviderDto
    {
        public int Id { get; set; }

        public int WorkspaceId { get; set; }

        public string Name { get; set; } = string.Empty;

        public ProviderTypes Type { get; set; }

        public ProviderDetailsDto? Details { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? LastModifiedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public string? LastModifiedBy { get; set; }

        public IEnumerable<ApiProviderDetailsDto> ApiProviderDetails { get; set; } = [];
    }

    public class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<ProviderDto, Provider>()
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.Details != null ? JsonConvert.SerializeObject(src.Details) : null));

            CreateMap<Provider, ProviderDto>()
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.Details) ? JsonConvert.DeserializeObject<ProviderDetailsDto>(src.Details) : null));
        }
    }
}
