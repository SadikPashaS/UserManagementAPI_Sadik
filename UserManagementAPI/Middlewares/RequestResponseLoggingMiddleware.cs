
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public sealed class RequestResponseLoggingMiddleware
{
    readonly RequestDelegate _next;
    readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;

        await _next(context);

        sw.Stop();
        var status = context.Response.StatusCode;
        _logger.LogInformation("HTTP {Method} {Path} => {Status} in {Elapsed} ms", method, path, status, sw.ElapsedMilliseconds);
    }
}
