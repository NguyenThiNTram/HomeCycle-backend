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
    public interface IInspectionCollectionService
    {
        Task<Result<ScheduleInspectionCollectionResponse>> ScheduleAsync(
            Guid inspectionFormId,
            Guid buyerId,
            ScheduleInspectionCollectionRequest request,
            CancellationToken cancellationToken = default);
    }
}
