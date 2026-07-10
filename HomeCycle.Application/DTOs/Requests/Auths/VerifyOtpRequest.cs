using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Auths
{
    public class VerifyOtpRequest
    {
        public string Email { get; set; } = null!;

        public string Otp { get; set; } = null!;
    }
}
