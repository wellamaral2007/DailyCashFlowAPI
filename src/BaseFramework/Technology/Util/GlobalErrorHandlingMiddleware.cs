// File: libs/BaseFramework/src/BaseFramework.Technology/Util/GlobalErrorHandlingMiddleware.cs
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BaseFramework.Technology.Util;

/**
 * ============================================================================
 * TOGAF DIMENSION: Phase D - Technology Architecture (Shared Kernel Infrastructure)
 * HEXAGONAL ROLE:  Inbound Pipeline Middleware / Exception Interceptor
 * CONCEPTS:        Global Exception Catching / RFC 7807 Error Formatting
 * ============================================================================
 * This middleware intercepts the ASP.NET Core / Kestrel pipeline execution chain.
 * It guarantees that any unhandled exception across serverless cloud functions 
 * is captured, logged, and formatted into clean corporate JSON payloads.
 */
public class GlobalErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    /**
     * Dependency Injection of the next pipeline execution delegate link.
     */
    public GlobalErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /**
     * Invokes the pipeline request execution flow inside a secure try-catch safety boundary.
     */
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Advances to the next execution block (Controllers, Functions, Filters)
            await _next(context);
        }
        catch (Exception ex)
        {
            // Captures any unhandled bubble-up exception immediately
            await HandleExceptionAsync(context, ex);
        }
    }

    /**
     * Translates system runtime exceptions into clear, predictable corporate error data envelopes.
     */
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var statusCode = HttpStatusCode.InternalServerError;
        var errorType = "Internal Server Error";

        // Business Layer / Hexagon Validation Boundary Mapping: Fail-Fast translations
        if (exception is ArgumentException || exception is InvalidOperationException)
        {
            statusCode = HttpStatusCode.BadRequest;
            errorType = "Bad Request / Validation Violation";
        }

        context.Response.StatusCode = (int)statusCode;

        // Standardized corporate error payload schema structure
        var errorResponse = new
        {
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            status = context.Response.StatusCode,
            error = errorType,
            message = exception.Message ?? "An unexpected corporate application error occurred."
        };

        // Serializes utilizing optimized native .NET JSON engine pipelines
        var result = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        // Logs structural details internally (Can be forwarded into centralized Cloud Logging meshes)
        Console.WriteLine($"[BASE FRAMEWORK EXCEPTION INTERCEPTOR] TraceId: {context.Items["TraceId"] ?? "N/A"} | Error: {errorType} | Reason: {exception.Message}");

        return context.Response.WriteAsync(result);
    }
}
