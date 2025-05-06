using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PadelPass.Core.Common;

namespace PadelPass.Infrastructure.ExceptionHandling;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger)
    {
        _next    = next;
        _logger  = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext ctx, Exception ex)
    {
        var resp = ApiResponse.Fail("An unexpected error occurred.");
        var result = JsonSerializer.Serialize(resp);

        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;
        return ctx.Response.WriteAsync(result);
    }
}