namespace SmartX.Shared;

public class TelemetryPacket<T>
{
    public int SensorId { get; set; }

    public T? Value { get; set; }

    public string Unit { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}