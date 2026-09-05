using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Security
{
    public interface IOtpProtectionService
    {
        bool TryStartSend(string email, out TimeSpan remaining);

        void SendSucceeded(string email);

        void SendFailed(string email);

        bool IsLocked(string email, out TimeSpan remaining);

        bool RegisterFailedAttempt(string email, out int failedAttempts, out TimeSpan lockRemaining);

        void Clear(string email);
    }
}
