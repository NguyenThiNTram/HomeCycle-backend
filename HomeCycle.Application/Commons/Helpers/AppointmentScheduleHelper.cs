using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Commons.Helpers
{
    public static class AppointmentScheduleHelper
    {
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
        private static readonly TimeSpan BusinessStartTime = TimeSpan.FromHours(8);
        private static readonly TimeSpan BusinessEndTime = TimeSpan.FromHours(22);

        public static bool IsWithinBusinessHours(DateTime scheduledAt)
        {
            var utc = scheduledAt.Kind switch
            {
                DateTimeKind.Utc => scheduledAt,
                DateTimeKind.Local => scheduledAt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(scheduledAt, DateTimeKind.Utc)
            };

            return IsWithinBusinessHours(
                utc.Add(VietnamOffset).TimeOfDay);
        }

        public static bool IsWithinBusinessHours(DateTimeOffset scheduledAt)
        {
            return IsWithinBusinessHours(
                scheduledAt.ToOffset(VietnamOffset).TimeOfDay);
        }

        private static bool IsWithinBusinessHours(TimeSpan vietnamTime)
        {
            return vietnamTime >= BusinessStartTime &&
                vietnamTime <= BusinessEndTime;
        }
    }
}
