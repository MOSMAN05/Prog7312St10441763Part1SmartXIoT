namespace SmartX.Shared;

public class AttachmentRecord
{
    public Guid Id { get; set; }

    public int SensorId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}