namespace Magnar.AI.Application.Models.Responses;
public class OdataResponse<T>
    where T : class
{
    public IEnumerable<T> Value { get; set; } = [];

    public int TotalCount { get; set; } = 0;
}
