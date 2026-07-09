using HomeCycle.Domain.Entities;
using Supabase.Gotrue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Security
{
    public interface IJwtService
    {
        string GenerateAccessToken(user user);
        string GenerateRefreshToken();
    }
}
