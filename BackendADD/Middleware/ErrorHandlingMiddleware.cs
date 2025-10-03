using System.Net;
using System.Text.Json;
using BackendADD.Models;

namespace BackendADD.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _env;

    public ErrorHandlingMiddleware(RequestDelegate next, IHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            object? detail = _env.IsDevelopment()
                ? new { ex.Message, ex.StackTrace }
                : null;

            var resp = ApiResponse<object?>.Error("Internal Server Error", detail, 500);

            var json = JsonSerializer.Serialize(resp, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await ctx.Response.WriteAsync(json);
        }
    }
}

public static class ErrorHandlingExtensions
{
    public static IApplicationBuilder UseApiErrorHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ErrorHandlingMiddleware>();
}
