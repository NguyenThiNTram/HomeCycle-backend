using System.Diagnostics;
using HomeCycle.Application.Commons.Results;
using Microsoft.Extensions.Logging;

namespace HomeCycle.Application.Commons.Helpers;

/// <summary>Logs workflow checkpoints without request bodies, credentials or payment payloads.</summary>
public sealed class AgreementFlowLog : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _operation;
    private readonly object? _reference;
    private readonly Stopwatch _timer = Stopwatch.StartNew();
    private string _lastStep = "Started";
    private bool _completed;

    public AgreementFlowLog(ILogger logger, string operation, object? reference)
    {
        _logger = logger;
        _operation = operation;
        _reference = reference;
        Step("Started");
    }

    public void Step(string step)
    {
        _lastStep = step;
        _logger.LogInformation("[Agreement] {Operation} | Reference={Reference} -> {Step}",
            _operation, _reference, step);
    }

    public T Result<T>(T result) where T : Result
    {
        _completed = true;
        _logger.LogInformation("[Agreement] {Operation} | Reference={Reference} -> {Outcome} | Code={Code} | LastStep={LastStep} | {ElapsedMs}ms",
            _operation, _reference, result.IsSuccess ? "Success" : "Failed", result.Error?.Code,
            _lastStep, _timer.ElapsedMilliseconds);
        return result;
    }

    public void Complete(string outcome)
    {
        _completed = true;
        _logger.LogInformation("[Agreement] {Operation} | Reference={Reference} -> {Outcome} | {ElapsedMs}ms",
            _operation, _reference, outcome, _timer.ElapsedMilliseconds);
    }

    public void Dispose()
    {
        if (!_completed)
            _logger.LogInformation("[Agreement] {Operation} | Reference={Reference} -> Exited without completion | LastStep={LastStep} | {ElapsedMs}ms",
                _operation, _reference, _lastStep, _timer.ElapsedMilliseconds);
    }
}
