using System.Text.Json.Serialization;

namespace Magnar.AI.Application.Dto.Providers
{
    public class ProviderDetailsDto
    {
        public SqlServerProviderDetailsDto? SqlServerConfiguration { get; set; }

        public ApiProviderAuthDetailsDto? ApiProviderAuthDetails { get; set; }
    }
}
