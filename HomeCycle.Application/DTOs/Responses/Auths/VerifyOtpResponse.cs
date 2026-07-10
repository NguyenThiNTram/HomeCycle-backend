using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Auths
{
    public class VerifyOtpResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string RegistrationToken { get; set; } // Token nằm ở đây!
    }
}
