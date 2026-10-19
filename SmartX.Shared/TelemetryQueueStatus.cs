
namespace SmartX.Shared;

// Tracks telemetry queue processing
public class TelemetryQueueStatus
{
    public int PendingCount { get; set; }

    public long TotalProcessed { get; set; }

    public int IndexedTimestampCount { get; set; }

    public DateTime? OldestPendingTimestamp { get; set; }

    public List<int> RecentlyProcessedSensorIds { get; set; } = new();
}
