namespace Magnar.AI.Application.Models;

public class Result
{
    public Result(bool isSuccess, IEnumerable<Error> errors)
    {
        if (isSuccess && errors.Any())
        {
            throw new InvalidOperationException();
        }

        if (!isSuccess && !errors.Any())
        {
            throw new InvalidOperationException();
        }

        Errors = errors.ToArray();
        Success = isSuccess;
    }

    public bool Success { get; }

    public Error[] Errors { get; }

    public static Result CreateSuccess() => new(true, []);

    public static Result CreateFailure(IEnumerable<Error> errors) => new(false, errors);
}
