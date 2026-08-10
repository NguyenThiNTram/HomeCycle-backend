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
            var entity = await _db.Appointments.AsNoTracking().FirstOrDefaultAsync(x => x.AgreementId == agreementId, ct);
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

        public async Task<PagedResult<appointment>> GetPagedByTypeAsync(
           AppointmentType type,
           Guid userId,
           bool isSeller,
           AppointmentSearchRequest request,
           CancellationToken ct = default)
        {
            var query = _db.Appointments
                .AsNoTracking()
                .Where(a => a.AppointmentType == (int)type)
                .Where(a => isSeller ? a.Agreement.SellerId == userId : a.Agreement.BuyerId == userId);

            if (request.Status.HasValue)
                query = query.Where(a => a.AppointmentStatus == (int)request.Status.Value);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
                query = query.Where(a => a.Agreement.Order != null
                    && a.Agreement.Order.ProductName != null
                    && a.Agreement.Order.ProductName.Contains(request.Keyword));

            query = query.OrderByDescending(a => a.CreatedAt);

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            return new PagedResult<appointment>
            {
                Items = items.Select(x => x.ToDomain()).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<InspectionAppointmentListItemDto>> GetPagedInspectionListAsync(
            Guid userId, bool isSeller, AppointmentSearchRequest request, CancellationToken ct = default)
        {
            var query = _db.Appointments
                .AsNoTracking()
                .Where(a => a.AppointmentType == (int)AppointmentType.Inspection)
                .Where(a => isSeller ? a.Agreement.SellerId == userId : a.Agreement.BuyerId == userId);

            if (request.Status.HasValue)
                query = query.Where(a => a.AppointmentStatus == (int)request.Status.Value);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
                query = query.Where(a => a.Inspection_Appointment != null
                    && a.Inspection_Appointment.InspectionAddress != null
                    && a.Inspection_Appointment.InspectionAddress.Contains(request.Keyword));

            query = query.OrderByDescending(a => a.CreatedAt);

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(a => new InspectionAppointmentListItemDto
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentStatus = a.AppointmentStatus,
                    InspectionDate = a.Inspection_Appointment != null ? a.Inspection_Appointment.InspectionDate : null,
                    InspectionAddress = a.Inspection_Appointment != null ? a.Inspection_Appointment.InspectionAddress : null,
                    IsCancelled = a.CancelledAt.HasValue,
                    BuyerCheckedIn = a.BuyerCheckAt.HasValue,
                    SellerCheckedIn = a.SellerCheckAt.HasValue,
                    CreatedAt = a.CreatedAt
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
            var query = _db.Appointments
                .AsNoTracking()
                .Where(a => a.AppointmentType == (int)AppointmentType.Collection)
                .Where(a => isSeller ? a.Agreement.SellerId == userId : a.Agreement.BuyerId == userId);

            if (request.Status.HasValue)
                query = query.Where(a => a.AppointmentStatus == (int)request.Status.Value);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
                query = query.Where(a => a.Collection_Appointment != null
                    && a.Collection_Appointment.PickupAddress != null
                    && a.Collection_Appointment.PickupAddress.Contains(request.Keyword));

            query = query.OrderByDescending(a => a.CreatedAt);

            var totalCount = await query.CountAsync(ct);
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
                    IsCancelled = a.CancelledAt.HasValue,
                    BuyerCheckedIn = a.BuyerCheckAt.HasValue,
                    SellerCheckedIn = a.SellerCheckAt.HasValue,
                    CreatedAt = a.CreatedAt
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
    }
}
