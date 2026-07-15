using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Auths
{
    public class RegisterBusinessAccountRequest
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;

    }
}
