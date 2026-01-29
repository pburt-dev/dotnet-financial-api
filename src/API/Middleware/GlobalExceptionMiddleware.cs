using System.Text.Json;
using Application.Common.Exceptions;
using Domain.Exceptions;
using FluentValidation;

namespace API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            FluentValidation.ValidationException ex => (
                StatusCodes.Status400BadRequest,
                new ErrorResponse("Validation failed", GetValidationErrors(ex))
            ),
            Application.Common.Exceptions.ValidationException ex => (
                StatusCodes.Status400BadRequest,
                new ErrorResponse("Validation failed", ex.Errors)
            ),
            NotFoundException ex => (
                StatusCodes.Status404NotFound,
                new ErrorResponse(ex.Message)
            ),
            InsufficientFundsException ex => (
                StatusCodes.Status422UnprocessableEntity,
                new ErrorResponse(ex.Message, new
                {
                    available = ex.Available.Amount,
                    availableCurrency = ex.Available.CurrencyCode,
                    requested = ex.Requested.Amount,
                    requestedCurrency = ex.Requested.CurrencyCode
                })
            ),
            AccountFrozenException ex => (
                StatusCodes.Status422UnprocessableEntity,
                new ErrorResponse("Account is frozen", new { accountId = ex.AccountId, reason = ex.Reason })
            ),
            AccountClosedException ex => (
                StatusCodes.Status422UnprocessableEntity,
                new ErrorResponse(ex.Message, new { accountId = ex.AccountId })
            ),
            CurrencyMismatchException ex => (
                StatusCodes.Status422UnprocessableEntity,
                new ErrorResponse(ex.Message, new { expected = ex.ExpectedCurrency, actual = ex.ActualCurrency })
            ),
            DuplicateIdempotencyKeyException ex => (
                StatusCodes.Status409Conflict,
                new ErrorResponse("Request already processed", new { idempotencyKey = ex.IdempotencyKey })
            ),
            DomainException ex => (
                StatusCodes.Status422UnprocessableEntity,
                new ErrorResponse(ex.Message)
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                new ErrorResponse("An unexpected error occurred")
            )
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception: {Message}", exception.Message);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }

    private static IDictionary<string, string[]> GetValidationErrors(FluentValidation.ValidationException ex)
    {
        return ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );
    }
}

public record ErrorResponse
{
    public string Error { get; init; }
    public object? Details { get; init; }

    public ErrorResponse(string error, object? details = null)
    {
        Error = error;
        Details = details;
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
