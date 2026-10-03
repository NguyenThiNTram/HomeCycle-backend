using HomeCycle.Application.Interfaces.Repositories.Appointments;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Appointments
{
    // Hàng đã tới tay người mua (người mua xác nhận, hệ thống tự hoàn tất đơn, hoặc kết luận tranh chấp
    // xác định người mua đã có hàng) thì lịch giao nhận còn mở phải chuyển sang Completed. Nếu không,
    // lịch vẫn "Đã lên lịch", vẫn bị nhắc lịch và thống kê giao nhận đếm thiếu.
    internal static class CollectionAppointmentCompletion
    {
        // Gọi trong transaction của nơi hoàn tất đơn. Trả về lịch vừa hoàn thành để nơi gọi phát realtime
        // sau khi commit; trả null khi đơn không có lịch giao nhận còn mở.
        public static async Task<appointment?> CompleteOpenAsync(
            IAppointmentRepository appointmentRepository,
            Guid agreementId,
            DateTime completedAt,
            CancellationToken ct)
        {
            var snapshot = await appointmentRepository.GetByAgreementIdAndTypeAsync(
                agreementId,
                AppointmentType.Collection,
                ct);

            if (snapshot == null)
                return null;

            var collection = await appointmentRepository.GetByIdForUpdateAsync(
                snapshot.AppointmentId,
                ct);

            if (collection?.AppointmentStatus is not
                ((int)AppointmentStatus.Scheduled or (int)AppointmentStatus.InProgress))
            {
                return null;
            }

            collection.AppointmentStatus = (int)AppointmentStatus.Completed;
            collection.CompletedAt = completedAt;
            collection.UpdatedAt = completedAt;

            await appointmentRepository.UpdateAsync(collection, ct);
            return collection;
        }
    }
}
