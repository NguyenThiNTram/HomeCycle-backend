using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Agreements
{
    public class AcceptAgreementRequest
    {
        // Revision mà client đang nhìn thấy lúc bấm "Đồng ý" — lấy từ AgreementDetailsDto.Revision
        // của lần GetDetailAsync gần nhất. Server so khớp với Revision thật tại thời điểm xử lý
        // để chặn trường hợp bên kia vừa cập nhật nội dung ngay trước đó (stale confirmation).
        public int ExpectedRevision { get; set; }
    }
}
