using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Appointments;
using HomeCycle.Application.DTOs.Responses.Appointments;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Appointments
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly HomeCycleDbContext _db;
        public AppointmentRepository(HomeCycleDbContext db) => _db = db;

        public async Task<appointment?> GetByIdAsync(Guid appointmentId, CancellationToken ct = default)
        {
            var entity = await _db.Appointments.AsNoTracking().FirstOrDefaultAsync(x => x.AppointmentId == appointmentId, ct);
            return entity?.ToDomain();
        }

        public async Task<appointment?> GetByAgreementIdAsync(Guid agreementId, CancellationToken ct = default)
        {
            var entity = await _db.Appointments
                .AsNoTracking()
                .Where(x => x.AgreementId == agreementId)
                .Where(x =>
                    x.AppointmentStatus == (int)AppointmentStatus.Scheduled ||
                    x.AppointmentStatus == (int)AppointmentStatus.InProgress ||
                    x.AppointmentStatus == (int)AppointmentStatus.Completed)
                .OrderBy(x =>
                    x.AppointmentStatus == (int)AppointmentStatus.Scheduled ||
                    x.AppointmentStatus == (int)AppointmentStatus.InProgress
                        ? 0
                        : 1)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);

            return entity?.ToDomain();
        }

        public async Task AddAsync(appointment appointment, CancellationToken ct = default)
        {
            await _db.Appointments.AddAsync(appointment.ToInfrastructure(), ct);
        }

        public Task UpdateAsync(appointment appointment, CancellationToken ct = default)
        {
            _db.Appointments.Update(appointment.ToInfrastructure());
            return Task.CompletedTask;
        }


        public async Task<PagedResult<InspectionAppointmentListItemDto>> GetPagedInspectionListAsync(
            Guid userId, bool isSeller, AppointmentSearchRequest request, CancellationToken ct = default)
        {
            var query = BuildBaseAppointmentQuery(AppointmentType.Inspection, userId, isSeller, request.Status);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
                query = query.Where(a => a.Inspection_Appointment != null
                    && a.Inspection_Appointment.InspectionAddress != null
                    && a.Inspection_Appointment.InspectionAddress.Contains(request.Keyword));


            query = query.OrderByDescending(a => a.CreatedAt);

            var totalCount = await query.CountAsync(ct);
            var now = DateTime.UtcNow;

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(a => new InspectionAppointmentListItemDto
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentStatus = a.AppointmentStatus,
                    InspectionDate = a.Inspection_Appointment != null ? a.Inspection_Appointment.InspectionDate : null,
                    InspectionAddress = a.Inspection_Appointment != null ? a.Inspection_Appointment.InspectionAddress : null,

                    BuyerCheckedIn = a.BuyerCheckAt.HasValue,
                    SellerCheckedIn = a.SellerCheckAt.HasValue,

                    LateThresholdAt = a.LateThresholdAt,

                    IsOverdue =
                        a.LateThresholdAt.HasValue &&
                        a.LateThresholdAt.Value <= now &&
                        (a.AppointmentStatus == (int)AppointmentStatus.Scheduled ||
                         a.AppointmentStatus == (int)AppointmentStatus.InProgress) &&
                        (!a.BuyerCheckAt.HasValue || !a.SellerCheckAt.HasValue),

                    IsCancelled = a.CancelledAt.HasValue,
                    CreatedAt = a.CreatedAt,
                    CounterpartyName = isSeller ? a.Agreement.Buyer.Username : a.Agreement.Seller.Username
                })
                .ToListAsync(ct); 

            return new PagedResult<InspectionAppointmentListItemDto>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }


        public async Task<PagedResult<CollectionAppointmentListItemDto>> GetPagedCollectionListAsync(
            Guid userId, bool isSeller, AppointmentSearchRequest request, CancellationToken ct = default)
        {
            var query = BuildBaseAppointmentQuery(AppointmentType.Collection, userId, isSeller, request.Status);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
                query = query.Where(a => a.Collection_Appointment != null
                    && a.Collection_Appointment.PickupAddress != null
                    && a.Collection_Appointment.PickupAddress.Contains(request.Keyword));


            query = query.OrderByDescending(a => a.CreatedAt);

            var totalCount = await query.CountAsync(ct);
            var now = DateTime.UtcNow;

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(a => new CollectionAppointmentListItemDto
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentStatus = a.AppointmentStatus,
                    CollectionDate = a.Collection_Appointment != null ? a.Collection_Appointment.CollectionDate : null,
                    PickupAddress = a.Collection_Appointment != null ? a.Collection_Appointment.PickupAddress : null,
                    DeliveryAddress = a.Collection_Appointment != null ? a.Collection_Appointment.DeliveryAddress : null,
                    DeliveryMethod = a.Collection_Appointment != null ? a.Collection_Appointment.DeliveryMethod : null,

                    LateThresholdAt = a.LateThresholdAt,

                    IsOverdue =
                        a.LateThresholdAt.HasValue &&
                        a.LateThresholdAt.Value <= now &&
                        (a.AppointmentStatus == (int)AppointmentStatus.Scheduled ||
                         a.AppointmentStatus == (int)AppointmentStatus.InProgress),

                    IsCancelled = a.CancelledAt.HasValue,
                    CreatedAt = a.CreatedAt,
                    CounterpartyName = isSeller ? a.Agreement.Buyer.Username : a.Agreement.Seller.Username
                })
                .ToListAsync(ct);

            return new PagedResult<CollectionAppointmentListItemDto>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<appointment?> GetByAgreementIdAndTypeAsync(Guid agreementId, AppointmentType appointmentType, CancellationToken ct = default)
        {
            var entity = await _db.Appointments
                .AsNoTracking()
                .Where(x => x.AgreementId == agreementId && x.AppointmentType == (int)appointmentType)
                .Where(x =>
                    x.AppointmentStatus == (int)AppointmentStatus.Scheduled ||
                    x.AppointmentStatus == (int)AppointmentStatus.InProgress ||
                    x.AppointmentStatus == (int)AppointmentStatus.Completed)
                .OrderBy(x =>
                    x.AppointmentStatus == (int)AppointmentStatus.Scheduled ||
                    x.AppointmentStatus == (int)AppointmentStatus.InProgress
                        ? 0
                        : 1)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);

            return entity?.ToDomain();
        }

        public async Task<IReadOnlyList<AppointmentSummaryDto>> GetAppointmentSummariesByAgreementIdAsync(Guid agreementId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            return await _db.Appointments
                .AsNoTracking()
                .Where(a => a.AgreementId == agreementId)
                .Where(a =>
                    a.AppointmentStatus == (int)AppointmentStatus.Scheduled ||
                    a.AppointmentStatus == (int)AppointmentStatus.InProgress ||
                    a.AppointmentStatus == (int)AppointmentStatus.Completed)
                .OrderBy(a => a.CreatedAt)
                .Select(a => new AppointmentSummaryDto
                {
                    AppointmentId = a.AppointmentId,

                    AppointmentType = a.AppointmentType.HasValue ? (AppointmentType?)a.AppointmentType.Value : null,
                    AppointmentStatus = a.AppointmentStatus.HasValue ? (AppointmentStatus?)a.AppointmentStatus.Value : null,

                    ScheduledAt = a.AppointmentType == (int)AppointmentType.Inspection
                        ? a.Inspection_Appointment != null ? a.Inspection_Appointment.InspectionDate : null
                        : a.Collection_Appointment != null ? a.Collection_Appointment.CollectionDate : null,

                    Location = a.AppointmentType == (int)AppointmentType.Inspection
                        ? a.Inspection_Appointment != null ? a.Inspection_Appointment.InspectionAddress : null
                        : a.Collection_Appointment != null ? a.Collection_Appointment.PickupAddress : null,

                    InspectionCheckIn = a.AppointmentType == (int)AppointmentType.Inspection
                        ? new InspectionCheckInSummaryDto
                        {
                            BuyerCheckAt = a.BuyerCheckAt,
                            SellerCheckAt = a.SellerCheckAt
                        }
                        : null,

                    LateThresholdAt = a.LateThresholdAt,

                    IsOverdue =
                        a.LateThresholdAt.HasValue &&
                        a.LateThresholdAt.Value <= now &&
                        (a.AppointmentStatus == (int)AppointmentStatus.Scheduled ||
                         a.AppointmentStatus == (int)AppointmentStatus.InProgress) &&
                        (
                            a.AppointmentType == (int)AppointmentType.Collection ||
                            !a.BuyerCheckAt.HasValue ||
                            !a.SellerCheckAt.HasValue
                        ),

                    CreatedAt = a.CreatedAt,
                    CompletedAt = a.CompletedAt
                })
                .ToListAsync(ct);
        }

        public async Task<appointment?> GetByIdForUpdateAsync(Guid appointmentId, CancellationToken ct = default)
        {
            var entity = await _db.Appointments
                .FromSqlInterpolated(
                    $"SELECT * FROM public.\"Appointment\" WHERE \"AppointmentId\" = {appointmentId} FOR UPDATE")
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);

            return entity?.ToDomain();
        }

        public async Task<appointment?> GetPendingRescheduleProposalAsync(Guid sourceAppointmentId, CancellationToken ct = default)
        {
            var entity = await _db.Appointments
                .AsNoTracking()
                .Where(x =>
                    x.RescheduledFromAppointmentId == sourceAppointmentId &&
                    x.AppointmentStatus == (int)AppointmentStatus.Proposed)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);

            return entity?.ToDomain();
        }


        public async Task<PagedResult<ModeratorAppointmentReadModel>> GetPagedForModeratorAsync(
            ModeratorAppointmentQuery request,
            CancellationToken ct = default)
        {
            var query = _db.Appointments.AsNoTracking().AsQueryable();

            if (request.Type.HasValue)
                query = query.Where(a => a.AppointmentType == (int)request.Type.Value);

            if (request.Status.HasValue)
                query = query.Where(a => a.AppointmentStatus == (int)request.Status.Value);

            if (request.OrderId.HasValue)
                query = query.Where(a => a.Agreement.Order != null && a.Agreement.Order.OrderId == request.OrderId.Value);

            if (request.BuyerId.HasValue)
                query = query.Where(a => a.Agreement.BuyerId == request.BuyerId.Value);

            if (request.SellerId.HasValue)
                query = query.Where(a => a.Agreement.SellerId == request.SellerId.Value);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();

                query = query.Where(a =>
                    EF.Functions.ILike(a.Agreement.Buyer.Username, $"%{keyword}%") ||
                    EF.Functions.ILike(a.Agreement.Seller.Username, $"%{keyword}%") ||
                    (a.Agreement.Order != null && EF.Functions.ILike(a.Agreement.Order.OrderCode, $"%{keyword}%")) ||
                    (a.Agreement.Order != null && a.Agreement.Order.ProductName != null &&
                     EF.Functions.ILike(a.Agreement.Order.ProductName, $"%{keyword}%")) ||
                    (a.Inspection_Appointment != null && a.Inspection_Appointment.InspectionAddress != null &&
                     EF.Functions.ILike(a.Inspection_Appointment.InspectionAddress, $"%{keyword}%")) ||
                    (a.Collection_Appointment != null && a.Collection_Appointment.PickupAddress != null &&
                     EF.Functions.ILike(a.Collection_Appointment.PickupAddress, $"%{keyword}%")));
            }

            if (request.HasInspectionForm.HasValue)
            {
                if (request.HasInspectionForm.Value)
                {
                    query = query.Where(a =>
                        a.Inspection_Appointment != null &&
                        a.Inspection_Appointment.Inspection_Form != null);
                }
                else
                {
                    query = query.Where(a =>
                        a.Inspection_Appointment == null ||
                        a.Inspection_Appointment.Inspection_Form == null);
                }
            }

            if (request.ScheduledFrom.HasValue)
            {
                var from = request.ScheduledFrom.Value;

                query = query.Where(a =>
                    (a.AppointmentType == (int)AppointmentType.Inspection &&
                     a.Inspection_Appointment != null &&
                     a.Inspection_Appointment.InspectionDate >= from) ||
                    (a.AppointmentType == (int)AppointmentType.Collection &&
                     a.Collection_Appointment != null &&
                     a.Collection_Appointment.CollectionDate >= from));
            }

            if (request.ScheduledTo.HasValue)
            {
                var to = request.ScheduledTo.Value;

                query = query.Where(a =>
                    (a.AppointmentType == (int)AppointmentType.Inspection &&
                     a.Inspection_Appointment != null &&
                     a.Inspection_Appointment.InspectionDate <= to) ||
                    (a.AppointmentType == (int)AppointmentType.Collection &&
                     a.Collection_Appointment != null &&
                     a.Collection_Appointment.CollectionDate <= to));
            }

            if (request.IsOverdue.HasValue)
            {
                if (request.IsOverdue.Value)
                {
                    query = query.Where(a =>
                        a.LateThresholdAt.HasValue &&
                        a.LateThresholdAt.Value < request.NowUtc &&
                        (a.AppointmentStatus == (int)AppointmentStatus.Scheduled ||
                         a.AppointmentStatus == (int)AppointmentStatus.InProgress) &&
                        (a.AppointmentType == (int)AppointmentType.Collection ||
                         !a.BuyerCheckAt.HasValue ||
                         !a.SellerCheckAt.HasValue));
                }
                else
                {
                    query = query.Where(a =>
                        !(a.LateThresholdAt.HasValue &&
                          a.LateThresholdAt.Value < request.NowUtc &&
                          (a.AppointmentStatus == (int)AppointmentStatus.Scheduled ||
                           a.AppointmentStatus == (int)AppointmentStatus.InProgress) &&
                          (a.AppointmentType == (int)AppointmentType.Collection ||
                           !a.BuyerCheckAt.HasValue ||
                           !a.SellerCheckAt.HasValue)));
                }
            }

            var totalCount = await query.CountAsync(ct);

            var items = await ProjectModeratorAppointments(query, request.NowUtc)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            return new PagedResult<ModeratorAppointmentReadModel>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public Task<ModeratorAppointmentReadModel?> GetForModeratorAsync(
            Guid appointmentId,
            CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            return ProjectModeratorAppointments(
                    _db.Appointments.AsNoTracking().Where(a => a.AppointmentId == appointmentId),
                    now)
                .FirstOrDefaultAsync(ct);
        }


        // ================ HELPER ===================
        private IQueryable<ModeratorAppointmentReadModel> ProjectModeratorAppointments(
            IQueryable<Appointment> query,
            DateTime nowUtc)
        {
            return query.Select(a => new ModeratorAppointmentReadModel
            {
                AppointmentId = a.AppointmentId,
                AgreementId = a.AgreementId,

                AppointmentType = a.AppointmentType.HasValue
                    ? (AppointmentType?)a.AppointmentType.Value
                    : null,

                AppointmentStatus = a.AppointmentStatus.HasValue
                    ? (AppointmentStatus?)a.AppointmentStatus.Value
                    : null,

                OrderId = a.Agreement.Order != null ? (Guid?)a.Agreement.Order.OrderId : null,
                OrderCode = a.Agreement.Order != null ? a.Agreement.Order.OrderCode : null,
                ProductName = a.Agreement.Order != null ? a.Agreement.Order.ProductName : null,

                BuyerId = a.Agreement.BuyerId,
                BuyerUsername = a.Agreement.Buyer.Username,
                BuyerAvatarUrl = a.Agreement.Buyer.AvatarUrl,

                SellerId = a.Agreement.SellerId,
                SellerUsername = a.Agreement.Seller.Username,
                SellerAvatarUrl = a.Agreement.Seller.AvatarUrl,

                ScheduledAt = a.AppointmentType == (int)AppointmentType.Inspection
                    ? a.Inspection_Appointment != null ? a.Inspection_Appointment.InspectionDate : null
                    : a.Collection_Appointment != null ? a.Collection_Appointment.CollectionDate : null,

                Location = a.AppointmentType == (int)AppointmentType.Inspection
                    ? a.Inspection_Appointment != null ? a.Inspection_Appointment.InspectionAddress : null
                    : a.Collection_Appointment != null ? a.Collection_Appointment.PickupAddress : null,

                BuyerCheckAt = a.BuyerCheckAt,
                SellerCheckAt = a.SellerCheckAt,
                LateThresholdAt = a.LateThresholdAt,

                IsOverdue =
                    a.LateThresholdAt.HasValue &&
                    a.LateThresholdAt.Value < nowUtc &&
                    (a.AppointmentStatus == (int)AppointmentStatus.Scheduled ||
                     a.AppointmentStatus == (int)AppointmentStatus.InProgress) &&
                    (a.AppointmentType == (int)AppointmentType.Collection ||
                     !a.BuyerCheckAt.HasValue ||
                     !a.SellerCheckAt.HasValue),

                InspectionFormId =
                    a.Inspection_Appointment != null && a.Inspection_Appointment.Inspection_Form != null
                        ? (Guid?)a.Inspection_Appointment.Inspection_Form.InspectionFormId
                        : null,

                InspectionStatus =
                    a.Inspection_Appointment != null &&
                    a.Inspection_Appointment.Inspection_Form != null &&
                    a.Inspection_Appointment.Inspection_Form.InspectionStatus.HasValue
                        ? (InspectionStatus?)a.Inspection_Appointment.Inspection_Form.InspectionStatus.Value
                        : null,

                InspectionConclusion =
                    a.Inspection_Appointment != null &&
                    a.Inspection_Appointment.Inspection_Form != null &&
                    a.Inspection_Appointment.Inspection_Form.Conclusion.HasValue
                        ? (InspectionConclusion?)a.Inspection_Appointment.Inspection_Form.Conclusion.Value
                        : null,

                CreatedAt = a.CreatedAt,
                CompletedAt = a.CompletedAt,
                UpdatedAt = a.UpdatedAt
            });
        }

        private IQueryable<Appointment> BuildBaseAppointmentQuery(
          AppointmentType type, Guid userId, bool isSeller, AppointmentStatus? status)
        {
            var query = _db.Appointments
                .AsNoTracking()
                .Where(a => a.AppointmentType == (int)type)
                .Where(a => isSeller ? a.Agreement.SellerId == userId : a.Agreement.BuyerId == userId)
                .Where(a =>
                    !(a.RescheduledFromAppointmentId != null &&
                      a.AppointmentStatus == (int)AppointmentStatus.Proposed));

            if (status.HasValue)
                query = query.Where(a => a.AppointmentStatus == (int)status.Value);

            return query;
        }

    }
}
