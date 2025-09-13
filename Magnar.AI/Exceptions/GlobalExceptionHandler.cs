using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Magnar.AI.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;

namespace Magnar.AI.Exceptions;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    public GlobalExceptionHandler()
    {
        exceptionHandlers = new()
            {
                { typeof(ValidationFailedException), HandleValidationException },
                { typeof(NotFoundException), HandleNotFoundException },
                { typeof(UnauthorizedAccessException), HandleUnauthorizedAccessException },
                { typeof(ForbiddenAccessException), HandleForbiddenAccessException },
            };
    }

    // TODO - Enforce naming rule on fields to not include prefix and be camel_case.
    private readonly Dictionary<Type, Func<HttpContext, Exception, Task>> exceptionHandlers;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var correlationId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        using var context = LogContext.PushProperty("CorrelationId", correlationId);

        try
        {
            await HandleExceptionAsync(httpContext, exception);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while handling an exception");
            await HandleGlobalException(httpContext, exception);
            return true;
        }
    }

    private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
    {
        var exceptionType = exception.GetType();

        if (exceptionHandlers.TryGetValue(exceptionType, out Func<HttpContext, Exception, Task>? value))
        {
            await value.Invoke(httpContext, exception);
        }
    }

    private async Task HandleGlobalException(HttpContext httpContext, Exception exception)
    {
        Log.Error(exception, "{Message}", exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails()
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Detail = exception.Message,
        });
    }

    private async Task HandleValidationException(HttpContext httpContext, Exception ex)
    {
        var exception = (ValidationFailedException)ex;
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        LogValidationErrors(exception);
        await httpContext.Response.WriteAsJsonAsync(new ValidationProblemDetails(exception.Errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        });
    }

    private async Task HandleNotFoundException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails()
        {
            Status = StatusCodes.Status404NotFound,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "A specified resource was not found.",
            Detail = ex.Message,
        });
    }

    private async Task HandleUnauthorizedAccessException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
        });
    }

    private async Task HandleForbiddenAccessException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
        });
    }

    private void LogValidationErrors(ValidationFailedException ex)
    {
        var message = string.Join("\n", ex.Errors.SelectMany(x => x.Value));
        Log.Error(ex, message);
    }
}
