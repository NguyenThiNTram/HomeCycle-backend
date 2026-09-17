namespace HomeCycle.Application.Interfaces.Services.SupplierMatching;

public interface ISupplierMatchMonitorService
{
    Task RunBatchAsync(
        int batchSize,
        decimal minimumScore,
        decimal improvementDelta,
        CancellationToken cancellationToken = default);
}
