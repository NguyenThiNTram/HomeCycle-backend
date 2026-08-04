using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum PaymentTransactionStatus
    {
        Pending = 0,
        Success = 1,
        Cancelled = 2,  
        Failed = 3    
    }
}
