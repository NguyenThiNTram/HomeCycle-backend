using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum OrderCompletionSource
    {
        BuyerConfirmed = 1,
        AutoConfirmed = 2,
        ModeratorResolved = 3
    }
}
