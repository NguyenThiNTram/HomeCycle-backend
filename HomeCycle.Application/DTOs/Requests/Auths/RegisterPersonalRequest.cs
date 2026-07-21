using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Auths
{
    public class RegisterPersonalRequest
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!; //confirm password sẽ được check ở phía client, nên không cần confirm password ở đây
        public string? PhoneNumber { get; set; }
        public IFormFile? AvatarUrl { get; set; }
        public string? FullName { get; set; }

        //điền thông tin CCCD (nếu có). Không bắt buộc, nhưng nếu có thì phải điền đầy đủ các thông tin
        public string? RepresentativeCode { get; set; }
        public string? RepresentativeName { get; set; }
        public DateOnly? RepresentativeDob { get; set; }
        public string? RepresentativeAddress { get; set; }
        public IFormFile? FrontIDCardImage { get; set; }
        public IFormFile? BackIDCardImage { get; set; }

        //điền thông tin ngân hàng (nếu có) Không bắt buộc
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountName { get; set; }
    }
}
