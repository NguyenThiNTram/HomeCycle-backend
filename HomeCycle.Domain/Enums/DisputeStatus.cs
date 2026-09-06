using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum DisputeStatus
    {
        Pending = 0,      // Mới gửi
        Resolved = 1,     // Đã giải quyết, có kết luận (ResolvedAt được set)
        Rejected = 2,     // Từ chối vì không hợp lệ/không đủ căn cứ
        Closed = 3,       // Đóng thủ công (vd người gửi tự rút đơn khiếu nại)
        UnderReview = 4,   // Moderator đã claim và đang xử lý dispute
        AwaitingReturn = 5
    }
}
