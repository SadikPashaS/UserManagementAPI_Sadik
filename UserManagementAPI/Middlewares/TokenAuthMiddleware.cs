
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public sealed class TokenAuthMiddleware
{
    readonly RequestDelegate _next;
    readonly string _token;
    readonly ILogger<TokenAuthMiddleware> _logger;

    public TokenAuthMiddleware(RequestDelegate next, string token, ILogger<TokenAuthMiddleware> logger)
    {
        _next = next;
        _token = token;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (path.StartsWith("/users", StringComparison.OrdinalIgnoreCase))
        {
            var header = context.Request.Headers.Authorization.ToString();
            var expected = $"Bearer {_token}";

            if (string.IsNullOrWhiteSpace(header) || !string.Equals(header, expected, StringComparison.Ordinal))
            {
                _logger.LogWarning("Unauthorized request: {Method} {Path}", context.Request.Method, path);

                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.ContentType = "application/json";

                var payload = JsonSerializer.Serialize(new { error = "Unauthorized" });
                await context.Response.WriteAsync(payload);
                return;
            }
        }

        await _next(context);
    }
}
