using Banking.Application.Exceptions;
using Banking.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Banking.API.Middleware;

/// <summary>
/// Catches exceptions from anywhere in the pipeline and turns them into a consistent
/// JSON error (RFC 7807 "ProblemDetails"). Controllers therefore need no try/catch blocks.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            var (status, title, detail) = ex switch
            {
                DomainException => (StatusCodes.Status400BadRequest, "Business rule violation", ex.Message),
                NotFoundException => (StatusCodes.Status404NotFound, "Not found", ex.Message),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict", ex.Message),
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrent update",
                    "The account was changed by another operation. Please reload and try again."),
                _ => (StatusCodes.Status500InternalServerError, "Server error",
                    "An unexpected error occurred.") // never leak internal details to the client
            };

            if (status == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            }
            else
            {
                _logger.LogWarning("Request {Method} {Path} failed: {Message}", context.Request.Method, context.Request.Path, ex.Message);
            }

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }
    }
}
