namespace Magnar.AI.Application.Exceptions;

public class ValidationFailedException : Exception
{
    public ValidationFailedException()
        : base(Constants.ExceptionMessages.ValidationError)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationFailedException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName ?? string.Empty, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}
