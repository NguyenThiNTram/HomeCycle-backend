using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Orders;
using HomeCycle.Application.Interfaces.Services.Orders;
using HomeCycle.Application.Interfaces.Services.Payments;
using Microsoft.Extensions.Logging;

namespace HomeCycle.Application.Services.Orders
{
    public sealed class AutoReleaseOrderProcessor : IOrderLifecycleProcessor
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<AutoReleaseOrderProcessor> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public string Name => "AutoReleaseOrder";

        public AutoReleaseOrderProcessor(
             IOrderRepository orderRepository,
             IPaymentService paymentService,
             IUnitOfWork unitOfWork,
             ILogger<AutoReleaseOrderProcessor> logger)
        {
            _orderRepository = orderRepository;
            _paymentService = paymentService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<int> ProcessDueAsync(
            int batchSize,
            CancellationToken ct = default)
        {
            var candidateIds =
                await _orderRepository.GetAutoReleaseCandidateIdsAsync(
                    DateTime.UtcNow,
                    batchSize,
                    ct);

            var processed = 0;

            foreach (var orderId in candidateIds)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var result =
                        await _paymentService.ReleaseCompletedOrderHeldAmountAsync(
                            orderId,
                            ct);

                    if (result.IsSuccess)
                    {
                        processed++;

                        _logger.LogInformation(
                            "Released held amount for Order {OrderId}: {Amount}.",
                            orderId,
                            result.Data);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Auto-release skipped/failed for Order {OrderId}: {Error}.",
                            orderId,
                            result.Error);
                    }
                }
                catch (OperationCanceledException)
                    when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Auto-release failed for Order {OrderId}.",
                        orderId);
                }
                finally
                {
                    _unitOfWork.ClearTrackedEntities();
                }
            }

            return processed;
        }
    }
}
