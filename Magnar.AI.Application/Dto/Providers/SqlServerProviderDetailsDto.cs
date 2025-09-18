namespace Magnar.AI.Application.Dto.Providers
{
    public class SqlServerProviderDetailsDto
    {
        public string InstanceName { get; set; } = string.Empty;

        public string DatabaseName { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
