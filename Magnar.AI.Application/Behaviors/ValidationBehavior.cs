namespace Magnar.AI.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        this.validators = validators;
    }

    private readonly IEnumerable<IValidator<TRequest>> validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ValidationContext<TRequest> context = new(request);

        ValidationResult[] validationFailures = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context)));

        List<ValidationFailure> errors = [.. validationFailures
            .Where(validationResult => !validationResult.IsValid)
            .SelectMany(validationResult => validationResult.Errors)
            .Select(validationFailure => new ValidationFailure(
                validationFailure.ErrorCode,
                validationFailure.ErrorMessage))];

        if (errors.Count != 0)
        {
            throw new ValidationFailedException(errors.DistinctBy(x => x.ErrorMessage));
        }

        TResponse response = await next(cancellationToken);

        return response;
    }
}