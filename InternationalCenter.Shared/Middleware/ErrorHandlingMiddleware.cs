using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using InternationalCenter.Shared.DTOs;
using FluentValidation;

namespace InternationalCenter.Shared.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing the request");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = exception switch
        {
            ValidationException validationEx => new ErrorResponse
            {
                Error = string.Join(", ", validationEx.Errors.Select(e => e.ErrorMessage)),
                Code = "VALIDATION_ERROR"
            },
            KeyNotFoundException => new ErrorResponse
            {
                Error = "Resource not found",
                Code = "NOT_FOUND"
            },
            UnauthorizedAccessException => new ErrorResponse
            {
                Error = "Unauthorized access",
                Code = "UNAUTHORIZED"
            },
            ArgumentException argEx => new ErrorResponse
            {
                Error = argEx.Message,
                Code = "BAD_REQUEST"
            },
            _ => new ErrorResponse
            {
                Error = "An internal server error occurred",
                Code = "INTERNAL_SERVER_ERROR"
            }
        };

        context.Response.StatusCode = exception switch
        {
            ValidationException => (int)HttpStatusCode.BadRequest,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var jsonResponse = JsonSerializer.Serialize(response, _jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }
}

public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}