
using System.ComponentModel.DataAnnotations;

namespace SmartX.Shared;

//  Represents telemetry received through the API
public class TelemetryEvent
{
    [Range(1, int.MaxValue)]
    public int SensorId { get; set; }

    [Required]
    public string DeviceName { get; set; } = string.Empty;

    [Required]
    public string Metric { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public bool IsAnomaly { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
