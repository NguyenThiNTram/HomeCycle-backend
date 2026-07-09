using HomeCycle.Application.Interfaces.Security;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Security
{
    public class PasswordHasherService : IPasswordHasher
    {
        private readonly IPasswordHasher<object> _passwordHasher;

        public PasswordHasherService(IPasswordHasher<object> passwordHasher)
        {
            _passwordHasher = passwordHasher;
        }

        public string HashPassword(string password) => _passwordHasher.HashPassword(null, password);

        public bool VerifyPassword(string providedPassword, string hashedPassword)
        {
            return _passwordHasher.VerifyHashedPassword(null, hashedPassword, providedPassword) == PasswordVerificationResult.Success;
        }
    }
}
