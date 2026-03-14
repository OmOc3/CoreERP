using ERP.Application.Common.Exceptions;
using ERP.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Middleware;

public sealed class ProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception for request {Path}", context.Request.Path);
            await WriteProblemDetailsAsync(context, exception, _environment);
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, Exception exception, IHostEnvironment environment)
    {
        var (statusCode, title, detail, errors) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "One or more validation errors occurred.",
                validationException.Errors
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(x => x.Key, x => x.Select(y => y.ErrorMessage).ToArray())),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", exception.Message, null),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden", exception.Message, null),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", exception.Message, null),
            DomainRuleException => (StatusCodes.Status400BadRequest, "Business rule violation", exception.Message, null),
            _ => (StatusCodes.Status500InternalServerError, "Server error", "An unexpected server error occurred.", null)
        };

        if (statusCode == StatusCodes.Status500InternalServerError &&
            (environment.IsDevelopment() || environment.IsEnvironment("Testing")))
        {
            detail = exception.Message;
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = context.Request.Path
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        if (errors == null)
        {
            await context.Response.WriteAsJsonAsync(problemDetails);
            return;
        }

        await context.Response.WriteAsJsonAsync(new ValidationProblemDetails(errors)
        {
            Status = problemDetails.Status,
            Title = problemDetails.Title,
            Detail = problemDetails.Detail,
            Type = problemDetails.Type,
            Instance = problemDetails.Instance
        });
    }
}
