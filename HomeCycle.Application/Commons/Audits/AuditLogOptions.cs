using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Commons.Audits
{
    public sealed class AuditLogOptions
    {
        public const string SectionName = "AuditLog";

        public AuditPayloadOptions Payload { get; set; } = new();
        public AuditContextOptions Context { get; set; } = new();
        public AuditWorkerOptions Worker { get; set; } = new();
        public AuditRetentionOptions Retention { get; set; } = new();
    }

    public sealed class AuditPayloadOptions
    {
        public int MaxBytes { get; set; } = 16384;
    }

    public sealed class AuditContextOptions
    {
        public bool CaptureIpAddress { get; set; } = true;
        public bool CaptureUserAgent { get; set; } = true;
    }

    public sealed class AuditWorkerOptions
    {
        public int BatchSize { get; set; } = 50;
        public int PollIntervalSeconds { get; set; } = 10;
        public int RetryLimit { get; set; } = 5;
        public int RetryBaseDelaySeconds { get; set; } = 30;
        public int ProcessingLeaseSeconds { get; set; } = 120;
    }

    public sealed class AuditRetentionOptions
    {
        public int Days { get; set; } = 365;
        public int CleanupBatchSize { get; set; } = 500;
        public int CleanupIntervalHours { get; set; } = 24;
    }
}
