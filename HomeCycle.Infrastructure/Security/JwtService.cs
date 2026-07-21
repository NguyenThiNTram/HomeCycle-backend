using HomeCycle.Application.Interfaces.Security;
using HomeCycle.Domain.Entities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Security
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateAccessToken(user user)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier,user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSection["AccessTokenMinutes"]!)),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            //return Convert.ToBase64String(randomNumber);
            return WebEncoders.Base64UrlEncode(randomNumber);
        }

        public string GenerateRegistrationToken(string email, string? avatarUrl = null, string provider = "Email")
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("token_use", "registration"),
            new Claim("provider", provider),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Dùng avt google nếu có
            if (!string.IsNullOrWhiteSpace(avatarUrl))
            {
                claims.Add(new Claim("avatar_url", avatarUrl));
            }

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(60), // Short-lived: 15 phút
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string ValidateRegistrationTokenAndGetEmail(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var jwtSection = _configuration.GetSection("Jwt");
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"],
                    ValidateLifetime = true,
                    // Xóa bỏ thời gian dung sai mặc định (thường là 5 phút) để token thực sự hết hạn đúng sau 15 phút
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;

                // Ngăn chặn việc dùng Access Token để bypass
                var tokenUseClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "token_use");
                if (tokenUseClaim == null || tokenUseClaim.Value != "registration")
                {
                    return null;
                }

                var emailClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email);
                return emailClaim?.Value;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string? GetAvatarFromRegistrationToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var jwtSection = _configuration.GetSection("Jwt");
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var tokenUse = jwtToken.Claims.FirstOrDefault(x => x.Type == "token_use")?.Value;

                if (tokenUse != "registration")
                    return null;

                return jwtToken.Claims.FirstOrDefault(x => x.Type == "avatar_url")?.Value;
            }
            catch
            {
                return null;
            }
        }
    }
}
