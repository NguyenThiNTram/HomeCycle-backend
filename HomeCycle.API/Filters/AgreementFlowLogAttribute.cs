using System.Diagnostics;
using HomeCycle.Application.Commons.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HomeCycle.API.Filters;

public sealed class AgreementFlowLogAttribute : TypeFilterAttribute
{
    public AgreementFlowLogAttribute() : base(typeof(AgreementFlowLogFilter))
    {
        // Run before ApiController's automatic model-validation filter.
        Order = -3000;
    }
}

public sealed class AgreementFlowLogFilter(ILogger<AgreementFlowLogFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var timer = Stopwatch.StartNew();
        var operation = $"{context.ActionDescriptor.RouteValues["controller"]}Controller.{context.ActionDescriptor.RouteValues["action"]}";
        object? reference = null;
        foreach (var key in new[] { "agreementId", "id", "negotiationId" })
            if (context.ActionArguments.TryGetValue(key, out reference) && reference != null)
                break;
        logger.LogInformation("[Agreement] {Operation} | Reference={Reference} -> Started", operation, reference);
        try
        {
            var executed = await next();
            if (executed.Exception != null && !executed.ExceptionHandled)
            {
                logger.LogError(executed.Exception, "[Agreement] {Operation} | Reference={Reference} -> Exception | {ElapsedMs}ms",
                    operation, reference, timer.ElapsedMilliseconds);
                return;
            }
            var status = executed.Result switch
            {
                ObjectResult result => result.StatusCode ?? StatusCodes.Status200OK,
                StatusCodeResult result => result.StatusCode,
                _ => context.HttpContext.Response.StatusCode
            };
            var error = (executed.Result as ObjectResult)?.Value as Error;
            logger.LogInformation("[Agreement] {Operation} | Reference={Reference} -> HTTP {StatusCode} | Code={Code} | {ElapsedMs}ms",
                operation, reference, status, error?.Code, timer.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("[Agreement] {Operation} -> Request cancelled | {ElapsedMs}ms", operation, timer.ElapsedMilliseconds);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "[Agreement] {Operation} -> Exception | {ElapsedMs}ms", operation, timer.ElapsedMilliseconds);
            throw;
        }
    }
}
