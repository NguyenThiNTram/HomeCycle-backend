namespace HomeCycle.API.Workers
{
    public sealed class OrderLifecycleWorkerOptions
    {
        public const string SectionName = "OrderLifecycleWorker";

        public int PollSeconds { get; set; }
        public int BatchSize { get; set; }
    }
}
