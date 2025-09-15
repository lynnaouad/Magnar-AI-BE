using Newtonsoft.Json;

namespace Magnar.AI.Application.Dto.Connection
{
    public class ConnectionDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public ProviderTypes Provider { get; set; }

        public bool IsDefault { get; set; }

        public ConnectionDetailsDto? Details { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? LastModifiedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public string? LastModifiedBy { get; set; }
    }

    public class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<ConnectionDto, Domain.Entities.Connection>()
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.Details != null ? JsonConvert.SerializeObject(src.Details) : null));

            CreateMap<Domain.Entities.Connection, ConnectionDto>()
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.Details) ? JsonConvert.DeserializeObject<ConnectionDetailsDto>(src.Details) : null));
        }
    }
}
