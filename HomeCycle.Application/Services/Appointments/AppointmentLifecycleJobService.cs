using HomeCycle.Application.Interfaces.Generics;
using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Application.Interfaces.Services.Appointments;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Appointments
{
    public class AppointmentLifecycleJobService
        : IAppointmentLifecycleJobService
    {
        private readonly IAppointmentRepository _appointmentRepo;
        private readonly IUnitOfWork _unitOfWork;

        public AppointmentLifecycleJobService(
            IAppointmentRepository appointmentRepo,
            IUnitOfWork unitOfWork)
        {
            _appointmentRepo = appointmentRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> ExpireOverdueAppointmentsAsync(
            CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var ids = await _appointmentRepo.GetOverdueCandidateIdsAsync(now, 100, ct);

            var expiredCount = 0;

            foreach (var appointmentId in ids)
            {
                await _unitOfWork.BeginTransactionAsync(ct);

                try
                {
                    var appointment = await _appointmentRepo.GetByIdForUpdateAsync(appointmentId, ct);

                    if (appointment == null)
                    {
                        await _unitOfWork.CommitTransactionAsync(ct);
                        continue;
                    }

                    var validStatus = appointment.AppointmentStatus == (int)AppointmentStatus.Scheduled || appointment.AppointmentStatus == (int)AppointmentStatus.InProgress;

                    var overdue = appointment.InteractionDeadlineAt.HasValue && appointment.InteractionDeadlineAt.Value <= now;

                    var fullyCheckedIn = appointment.BuyerCheckAt.HasValue && appointment.SellerCheckAt.HasValue;

                    if (!validStatus || !overdue || fullyCheckedIn)
                    {
                        await _unitOfWork.CommitTransactionAsync(ct);
                        continue;
                    }

                    appointment.AppointmentStatus = (int)AppointmentStatus.Expired;

                    appointment.UpdatedAt = now;

                    var proposal = await _appointmentRepo.GetPendingRescheduleProposalAsync(appointment.AppointmentId, ct);

                    if (proposal != null)
                    {
                        var lockedProposal = await _appointmentRepo.GetByIdForUpdateAsync(proposal.AppointmentId, ct);

                        if (lockedProposal?.AppointmentStatus == (int)AppointmentStatus.Proposed)
                        {
                            lockedProposal.AppointmentStatus = (int)AppointmentStatus.Cancelled;

                            lockedProposal.CancelledAt = now;
                            lockedProposal.CancellationReason = "Source appointment expired.";

                            lockedProposal.UpdatedAt = now;

                            await _appointmentRepo.UpdateAsync(
                                lockedProposal,
                                ct);
                        }
                    }

                    await _appointmentRepo.UpdateAsync(appointment, ct);

                    await _unitOfWork.SaveChangesAsync(ct);
                    await _unitOfWork.CommitTransactionAsync(ct);

                    expiredCount++;
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    throw;
                }
            }

            return expiredCount;
        }
    }
}
