using HomeCycle.Application.DTOs.Responses.Disputes;
using HomeCycle.Application.Interfaces.Services.Disputes;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Disputes
{
    public sealed class DisputeTimelineBuilder : IDisputeTimelineBuilder
    {
        public IReadOnlyList<DisputeTimelineStepDto> Build(
            DisputeDetailResponse detail)
        {
            ArgumentNullException.ThrowIfNull(detail);

            var steps = new List<DisputeTimelineStepDto>
            {
                CreateStep(
                    "dispute_created",
                    "Tranh chấp được tạo",
                    detail.Origin switch
                    {
                        DisputeOrigin.InspectionNoShow =>
                            "Hệ thống ghi nhận sự cố không đủ check-in tại lịch kiểm định.",

                        DisputeOrigin.CollectionNoShow =>
                            "Hệ thống ghi nhận sự cố giao nhận quá thời gian chờ.",

                        DisputeOrigin.InspectionRejected =>
                            "Tranh chấp được mở sau khi kết quả kiểm định bị từ chối.",

                        _ =>
                            "Người dùng đã gửi yêu cầu tranh chấp."
                    },
                    detail.CreatedAt)
            };

            if (detail.ResponseDeadlineAt.HasValue)
            {
                steps.Add(
                    CreateStep(
                        "response_window_opened",
                        "Bắt đầu thời hạn phản hồi",
                        $"Hạn phản hồi: {detail.ResponseDeadlineAt.Value:O}",
                        detail.CreatedAt));
            }

            foreach (var response in detail.Responses.OrderBy(x => x.CreatedAt))
            {
                var responseStep = response.ResponseType switch
                {
                    DisputeResponseType.Accept =>
                        CreateStep(
                            "counterparty_accepted",
                            "Phương án được chấp nhận",
                            "Bên phản hồi đã chấp nhận phương án giải quyết.",
                            response.CreatedAt),

                    DisputeResponseType.Rebut =>
                        CreateStep(
                            "counterparty_rebutted",
                            "Bên còn lại phản biện",
                            "Bên phản hồi đã bác bỏ phương án và gửi phản hồi chính thức.",
                            response.CreatedAt),

                    DisputeResponseType.Statement =>
                        CreateStep(
                            "statement_submitted",
                            "Đã bổ sung thông tin",
                            "Một bên đã gửi phát biểu bổ sung cho sự cố hệ thống.",
                            response.CreatedAt),

                    _ => null
                };

                if (responseStep != null)
                    steps.Add(responseStep);
            }

            if (detail.EscalatedAt.HasValue)
            {
                steps.Add(
                    CreateStep(
                        "dispute_escalated",
                        "Chuyển Moderator xử lý",
                        "Tranh chấp đã được chuyển sang hàng chờ Moderator.",
                        detail.EscalatedAt.Value));
            }

            if (detail.ModeratorClaimedAt.HasValue)
            {
                steps.Add(
                    CreateStep(
                        "moderator_claimed",
                        "Moderator tiếp nhận",
                        "Moderator đã tiếp nhận tranh chấp để xem xét.",
                        detail.ModeratorClaimedAt.Value));
            }

            var order = detail.Target.Order;

            if (order?.BuyerReturnConfirmedAt.HasValue == true)
            {
                steps.Add(
                    CreateStep(
                        "buyer_return_confirmed",
                        "Người mua xác nhận trả hàng",
                        "Người mua đã xác nhận hoàn trả sản phẩm.",
                        order.BuyerReturnConfirmedAt.Value));
            }

            if (order?.SellerReturnReceivedAt.HasValue == true)
            {
                steps.Add(
                    CreateStep(
                        "seller_return_received",
                        "Người bán xác nhận nhận hàng trả",
                        "Người bán đã xác nhận nhận lại sản phẩm.",
                        order.SellerReturnReceivedAt.Value));
            }

            AddFinalStateStep(steps, detail);

            return steps
                .OrderBy(x => x.OccurredAt)
                .ToArray();
        }

        private static void AddFinalStateStep(
            ICollection<DisputeTimelineStepDto> steps,
            DisputeDetailResponse detail)
        {
            if (detail.Status == DisputeStatus.Resolved)
            {
                steps.Add(
                    CreateStep(
                        "dispute_resolved",
                        "Tranh chấp đã được giải quyết",
                        detail.ResolutionSource switch
                        {
                            DisputeResolutionSource.MutualAgreement =>
                                "Hai bên đã thống nhất phương án giải quyết.",

                            DisputeResolutionSource.ModeratorDecision =>
                                "Moderator đã đưa ra quyết định cuối cùng.",

                            _ =>
                                "Tranh chấp đã hoàn tất."
                        },
                        detail.ResolvedAt ?? detail.UpdatedAt));

                return;
            }

            if (detail.Status == DisputeStatus.Rejected)
            {
                steps.Add(
                    CreateStep(
                        "dispute_rejected",
                        "Tranh chấp bị từ chối",
                        "Moderator đã từ chối tranh chấp.",
                        detail.ResolvedAt ?? detail.UpdatedAt));

                return;
            }

            if (detail.Status == DisputeStatus.Closed)
            {
                var description =
                    detail.ResolutionSource ==
                    DisputeResolutionSource.SystemAutoClosed
                        ? "Hệ thống tự đóng sự cố sau khi giao dịch tiếp tục thành công."
                        : "Tranh chấp đã được đóng.";

                steps.Add(
                    CreateStep(
                        "dispute_closed",
                        "Tranh chấp đã đóng",
                        description,
                        detail.UpdatedAt));
            }
        }

        private static DisputeTimelineStepDto CreateStep(
            string code,
            string title,
            string? description,
            DateTime occurredAt)
        {
            return new DisputeTimelineStepDto
            {
                Code = code,
                Title = title,
                Description = description,
                OccurredAt = occurredAt
            };
        }
    }
}
