using HomeCycle.Application.Interfaces.Security;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals
{
    public class OtpProtectionService : IOtpProtectionService
    {
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan SendCooldown = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

        private sealed class OtpState
        {
            public int FailedAttempts { get; set; }
            public DateTimeOffset? LockedUntil { get; set; }
        }

        private readonly ConcurrentDictionary<string, DateTimeOffset> _sendCooldowns = new();

        private readonly ConcurrentDictionary<string, OtpState> _otpStates = new();

        private readonly object _sync = new();

        public bool TryStartSend(string email, out TimeSpan remaining)
        {
            email = Normalize(email);
            var now = DateTimeOffset.UtcNow;

            lock (_sync)
            {
                if (_sendCooldowns.TryGetValue(email, out var cooldownUntil) && cooldownUntil > now)
                {
                    remaining = cooldownUntil - now;
                    return false;
                }

                // Đặt cooldown ngay khi request bắt đầu -> Nếu gửi email thất bại thì gọi SendFailed để xóa
                _sendCooldowns[email] = now.Add(SendCooldown);
                remaining = TimeSpan.Zero;
                return true;
            }
        }

        public void SendSucceeded(string email)
        {
            // Cooldown + Reset số lần nhập sai sau khi gửi OTP mới thành công
            email = Normalize(email);
            _otpStates.TryRemove(email, out _);
        }

        public void SendFailed(string email)
        {
            email = Normalize(email);
            _sendCooldowns.TryRemove(email, out _);
        }

        public bool IsLocked(string email, out TimeSpan remaining)
        {
            email = Normalize(email);
            var now = DateTimeOffset.UtcNow;

            lock (_sync)
            {
                if (!_otpStates.TryGetValue(email, out var state)
                    || state.LockedUntil is null)
                {
                    remaining = TimeSpan.Zero;
                    return false;
                }

                if (state.LockedUntil <= now)
                {
                    state.FailedAttempts = 0;
                    state.LockedUntil = null;

                    remaining = TimeSpan.Zero;
                    return false;
                }

                remaining = state.LockedUntil.Value - now;
                return true;
            }
        }

        public bool RegisterFailedAttempt(string email, out int failedAttempts, out TimeSpan lockRemaining)
        {
            email = Normalize(email);
            var now = DateTimeOffset.UtcNow;

            lock (_sync)
            {
                var state = _otpStates.GetOrAdd(
                    email,
                    _ => new OtpState());

                if (state.LockedUntil is not null && state.LockedUntil > now)
                {
                    failedAttempts = state.FailedAttempts;
                    lockRemaining = state.LockedUntil.Value - now;
                    return true;
                }

                state.FailedAttempts++;

                if (state.FailedAttempts >= MaxFailedAttempts)
                {
                    state.LockedUntil = now.Add(LockDuration);

                    failedAttempts = state.FailedAttempts;
                    lockRemaining = LockDuration;

                    return true;
                }

                failedAttempts = state.FailedAttempts;
                lockRemaining = TimeSpan.Zero;

                return false;
            }
        }

        public void Clear(string email)
        {
            email = Normalize(email);
            _otpStates.TryRemove(email, out _);
        }

        private static string Normalize(string email)
        {
            return email.Trim().ToLowerInvariant();
        }
    }
}
