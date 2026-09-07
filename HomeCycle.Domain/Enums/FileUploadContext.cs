using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum FileUploadContext
    {
        Avatar = 1,
        IdentityDocument = 2,
        BusinessDocument = 3,
        PostMedia = 4,
        ReviewMedia = 5,
        InspectionEvidence = 6,
        DisputeEvidence = 7
    }
}
