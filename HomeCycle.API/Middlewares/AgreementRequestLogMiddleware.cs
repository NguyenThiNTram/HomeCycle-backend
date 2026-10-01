using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace HomeCycle.API.Middlewares;

public sealed class AgreementRequestLogMiddleware(RequestDelegate next, ILogger<AgreementRequestLogMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/agreements", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var timer = Stopwatch.StartNew();
        logger.LogInformation("[Agreement] Request received -> {Method} {Path}", context.Request.Method, context.Request.Path);
        try
        {
            await next(context);
        }
        finally
        {
            var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
            var outcome = context.RequestAborted.IsCancellationRequested ? "Request aborted" : context.Response.StatusCode switch
            {
                401 => "Authentication required or token invalid/expired",
                403 => "Access forbidden",
                404 => "Route or resource not found",
                400 => "Bad request; check validation or business error response",
                _ => "Request finished"
            };
            logger.LogInformation("[Agreement] {Method} {Path} -> {Outcome} | Controller={Controller} | Action={Action} | HTTP {StatusCode} | {ElapsedMs}ms",
                context.Request.Method, context.Request.Path, outcome, action?.ControllerName ?? "Unmatched",
                action?.ActionName ?? "Unmatched", context.Response.StatusCode, timer.ElapsedMilliseconds);
        }
    }
}
