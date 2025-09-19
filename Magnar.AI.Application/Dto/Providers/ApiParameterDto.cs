namespace Magnar.AI.Application.Dto.Providers
{
    public class ApiParameterDto
    {
        public string Name { get; set; } = string.Empty;

        public ApiParameterDataType Type { get; set; } = ApiParameterDataType.String;

        public string Description { get; set; } = string.Empty;

        public bool Required { get; set; } = false;

        public ApiParameterLocation Location { get; set; } = ApiParameterLocation.Query;
    }
}
