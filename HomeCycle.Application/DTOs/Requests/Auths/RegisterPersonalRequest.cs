using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Auths
{
    //public class RegisterPersonalRequest
    //{
    //    public string Username { get; set; } = string.Empty;
    //    public string Email { get; set; } = string.Empty;
    //    public string Password { get; set; } = string.Empty;

    //    public string? FullName { get; set; }
    //    public string? PhoneNumber { get; set; }
    //    public string? AvatarUrl { get; set; }
        
    //    public string? RepresentativeCode { get; set; }
    //    public string? RepresentativeName { get; set; }
    //    public DateOnly? RepresentativeDob { get; set; }
    //    public string? RepresentativeAddress { get; set; }
    //    public string? FrontIDCardImage { get; set; }
    //    public string? BackIDCardImage { get; set; }

    //}

    public class RegisterPersonalRequest
    {
        //public string Email { get; set; } = null!; //Không hiển thị input email ở bước 2. Nên email sẽ được điền ở bước 1 và gửi sang bước 2
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!; //confirm password sẽ được check ở phía client, nên không cần confirm password ở đây
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string? FullName { get; set; }

        //điền thông tin CCCD (nếu có). Không bắt buộc, nhưng nếu có thì phải điền đầy đủ các thông tin
        public string? RepresentativeCode { get; set; }
        public string? RepresentativeName { get; set; }
        public DateOnly? RepresentativeDob { get; set; }
        public string? RepresentativeAddress { get; set; }
        public string? FrontIDCardImage { get; set; }
        public string? BackIDCardImage { get; set; }

        //điền thông tin ngân hàng (nếu có) Không bắt buộc
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
    }
}
