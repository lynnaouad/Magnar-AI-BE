namespace Magnar.AI.Application.Dto.Providers
{
    public class ProviderDetailsDto
    {
        public SqlServerProviderDetailsDto? SqlServerConfiguration { get; set; }

        public IEnumerable<ApiProviderDetailsDto> ApiProviderDetails { get; set; } = [];
    }
}
