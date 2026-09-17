namespace HomeCycle.API.Workers;

public sealed class SupplierMatchMonitorWorkerOptions
{
    public const string SectionName = "SupplierMatchMonitor";
    public bool Enabled { get; set; } = true;
    public int PollMinutes { get; set; } = 15;
    public int BatchSize { get; set; } = 50;
    public decimal MinimumScore { get; set; } = 6m;
    public decimal ImprovementDelta { get; set; } = 0.25m;
}
