using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum AuditSource
    {
        HttpApi = 1,
        Webhook = 2,
        BackgroundJob = 3,
        Internal = 4
    }
}
