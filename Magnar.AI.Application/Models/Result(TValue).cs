namespace Magnar.AI.Application.Models;

public class Result<TValue> : Result
    where TValue : notnull
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, IEnumerable<Error> error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => Success
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can not be accessed.");

    public static Result<TValue> CreateSuccess(TValue value) => new(value, true, []);

    public static new Result<TValue> CreateFailure(IEnumerable<Error> errors) => new(default, false, errors);
}
