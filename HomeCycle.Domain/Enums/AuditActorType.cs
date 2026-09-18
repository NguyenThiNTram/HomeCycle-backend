using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum AuditActorType
    {
        User = 1,
        Anonymous = 2,
        System = 3,
        ExternalSystem = 4
    }
}
