using Magnar.AI.Application.Dto.Providers;
using Newtonsoft.Json;

namespace Magnar.AI.Domain.Entities
{
    public class ApiProviderDetailsDto
    {
        public int Id { get; set; }

        public int ProviderId { get; set; } 

        public string PluginName { get; set; } = string.Empty;

        public string FunctionName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string ApiUrl { get; set; } = string.Empty;

        public HttpMethods HttpMethod { get; set; }

        public string? Payload { get; set; }

        public IEnumerable<ApiParameterDto> Parameters { get; set; } = [];

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? LastModifiedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public string? LastModifiedBy { get; set; }
    }

    public class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<ApiProviderDetails, ApiProviderDetailsDto>()
             .ForMember(dest => dest.Parameters,
                 opt => opt.MapFrom(src =>
                     string.IsNullOrWhiteSpace(src.ParametersJson)
                         ? new List<ApiParameterDto>()
                         : JsonConvert.DeserializeObject<List<ApiParameterDto>>(src.ParametersJson) ?? new List<ApiParameterDto>()
                 ));

            CreateMap<ApiProviderDetailsDto, ApiProviderDetails>()
                .ForMember(dest => dest.ParametersJson,
                    opt => opt.MapFrom(src =>
                        (src.Parameters != null && src.Parameters.Any())
                            ? JsonConvert.SerializeObject(src.Parameters)
                            : null
                    ));
        }
    }
}
