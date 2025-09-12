namespace Magnar.AI.Application.Models;

public record Error(string Message, Exception? Exception = default)
{
    public static readonly Error None = new(string.Empty);
}
