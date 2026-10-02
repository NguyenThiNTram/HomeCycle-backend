namespace HomeCycle.API.Workers
{
    public sealed class AppointmentReminderWorkerOptions
    {
        public const string SectionName =
            "AppointmentReminderWorker";

        public int PollSeconds { get; set; }

        public int BatchSize { get; set; }
    }
}
