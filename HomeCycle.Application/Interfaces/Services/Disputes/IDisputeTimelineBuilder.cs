using HomeCycle.Application.DTOs.Responses.Disputes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Disputes
{
    public interface IDisputeTimelineBuilder
    {
        IReadOnlyList<DisputeTimelineStepDto> Build(DisputeDetailResponse detail);
    }
}
