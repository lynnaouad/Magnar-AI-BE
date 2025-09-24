using Microsoft.AspNetCore.Http;

namespace Magnar.AI.Application.Models;

public class Result
{
    public Result(bool isSuccess, IEnumerable<Error> errors, int statusCode)
    {
        if (isSuccess && errors.Any())
        {
            throw new InvalidOperationException("A successful result cannot contain errors.");
        }

        if (!isSuccess && !errors.Any())
        {
            throw new InvalidOperationException("A failed result must contain at least one error.");
        }

        Errors = errors.ToArray();
        Success = isSuccess;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Collection of errors if the operation failed.
    /// </summary>
    public Error[] Errors { get; }

    /// <summary>
    /// Associated HTTP status code.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Creates a success result with 200 OK by default.
    /// </summary>
    public static Result CreateSuccess(int statusCode = StatusCodes.Status200OK) =>
        new(true, [], statusCode);

    /// <summary>
    /// Creates a failure result with 400 Bad Request by default.
    /// </summary>
    public static Result CreateFailure(IEnumerable<Error> errors, int statusCode = StatusCodes.Status400BadRequest) =>
        new(false, errors, statusCode);
}
