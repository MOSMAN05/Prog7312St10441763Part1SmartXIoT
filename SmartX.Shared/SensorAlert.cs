namespace SmartX.Shared;

public class SensorAlert
{
    public int Id { get; set; }

    public int SensorId { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}