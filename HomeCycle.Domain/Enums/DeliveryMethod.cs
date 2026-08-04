using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum DeliveryMethod
    {
        Unknown = 0, // để test - nếu chạy không xoá
        GhnDelivery = 1,
        SellerDelivers = 2,
        BuyerPickUp = 3
    }
}
