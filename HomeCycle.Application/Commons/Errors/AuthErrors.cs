using HomeCycle.Application.Commons.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Commons.Errors
{
    public static class AuthErrors
    {
        public static readonly Error EmailExists = new("AUTH_EMAIL_EXISTS", "Email already exists.");

        public static readonly Error UsernameExists = new("AUTH_USERNAME_EXISTS", "Username already exists.");

        public static readonly Error InvalidCredential = new("AUTH_INVALID_CREDENTIAL","Invalid username or password.");

        public static readonly Error UserNotFound = new("AUTH_USER_NOT_FOUND", "User not found.");

        public static readonly Error InvalidRefreshToken = new("AUTH_INVALID_REFRESH_TOKEN", "Refresh token is invalid.");

        public static readonly Error ExpiredRefreshToken = new("AUTH_REFRESH_TOKEN_EXPIRED", "Refresh token has expired.");

        public static readonly Error RevokedRefreshToken = new("AUTH_REFRESH_TOKEN_REVOKED", "Refresh token has been revoked.");

        public static readonly Error EmailNotVerified = new("AUTH_EMAIL_NOT_VERIFIED", "Email has not been verified.");

        public static readonly Error InvalidOtp = new("AUTH_INVALID_OTP", "Invalid OTP.");
    }
}
