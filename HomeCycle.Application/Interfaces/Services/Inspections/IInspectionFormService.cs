using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Inspections;
using HomeCycle.Application.DTOs.Responses.Inspections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Inspections
{
    public interface IInspectionFormService
    {
        Task<Result<InspectionFormResponseDto>> GetByAppointmentAsync(Guid appointmentId, Guid userId, CancellationToken ct = default);

        Task<Result<InspectionFormResponseDto>> CreateDraftAsync(Guid appointmentId, Guid buyerId, CreateInspectionFormRequest request, CancellationToken ct = default);
        Task<Result<InspectionFormResponseDto>> UpdateDraftAsync(Guid inspectionFormId, Guid buyerId, UpdateInspectionFormRequest request, CancellationToken ct = default);

        Task<Result<InspectionFormResponseDto>> SubmitAsync(Guid inspectionFormId, Guid buyerId, InspectionRevisionRequest request, CancellationToken ct = default);

        Task<Result<InspectionFormResponseDto>> SellerConfirmAsync(Guid inspectionFormId, Guid sellerId, InspectionRevisionRequest request, CancellationToken ct = default);
        Task<Result<InspectionFormResponseDto>> SellerRejectAsync(Guid inspectionFormId, Guid sellerId, RejectInspectionFormRequest request, CancellationToken ct = default);

        Task<Result<InspectionFormResponseDto>> CollectNowAsync(Guid inspectionFormId, Guid buyerId, InspectionRevisionRequest request, CancellationToken ct = default);

        Task<Result<InspectionFormResponseDto>> CancelTransactionAsync(Guid inspectionFormId, Guid userId, InspectionRevisionRequest request, CancellationToken ct = default);
    }
}
