using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BaseFramework.Technology.Util;

/// <summary>
/// Global exception interception middleware handling unhandled runtime boundary failures.
/// </summary>
public class GlobalErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance with the next request pipeline handler link.
    /// </summary>
    public GlobalErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>
    /// Intercepts the HTTP request context pipeline within a try-catch isolation boundary.
    /// </summary>
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

    /// <summary>
    /// Maps runtime application exceptions into structured corporate error JSON payloads.
    /// </summary>
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var statusCode = HttpStatusCode.InternalServerError;
        var errorType = "Internal Server Error";

        if (exception is ArgumentException || exception is InvalidOperationException)
        {
            statusCode = HttpStatusCode.BadRequest;
            errorType = "Bad Request / Validation Violation";
        }

        context.Response.StatusCode = (int)statusCode;

        var errorResponse = new
        {
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            status = context.Response.StatusCode,
            error = errorType,
            message = exception.Message ?? "An unexpected corporate application error occurred."
        };

        var result = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        Console.WriteLine($"[BASE FRAMEWORK EXCEPTION INTERCEPTOR] TraceId: {context.Items["TraceId"] ?? "N/A"} | Error: {errorType} | Reason: {exception.Message}");

        return context.Response.WriteAsync(result);
    }
}
