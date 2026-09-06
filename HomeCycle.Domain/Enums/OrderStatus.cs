using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum OrderStatus
    {
        Pending = 0,   
        Processing = 1, 
        Completed = 2,    
        Cancelled = 3,       
        Disputing = 4,
        Returned = 5
    }
}
