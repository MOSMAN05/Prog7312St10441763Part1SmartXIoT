namespace SmartX.Shared;

public class TelemetryRecord
{
    public int SensorId { get; set; }

    public string DeviceName { get; set; } = string.Empty;

    public string Metric { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public bool IsAnomaly { get; set; }

    public DateTime Timestamp { get; set; }
}