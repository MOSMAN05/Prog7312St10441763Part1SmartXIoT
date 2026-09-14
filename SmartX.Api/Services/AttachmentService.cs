using Microsoft.AspNetCore.DataProtection;
using SmartX.Shared;

namespace SmartX.Api.Services;

public class AttachmentService
{
    private readonly string uploadFolder;
    private readonly IDataProtector protector;
    private readonly List<AttachmentRecord> attachments = new();

    private readonly HashSet<string> allowedExtensions = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".log",
        ".json",
        ".jpg",
        ".jpeg",
        ".png",
        ".pdf"
    };

    public AttachmentService(
        IWebHostEnvironment environment,
        IDataProtectionProvider provider)
    {
        uploadFolder = Path.Combine(
            environment.ContentRootPath,
            "App_Data",
            "Uploads");

        Directory.CreateDirectory(uploadFolder);

        protector = provider.CreateProtector(
            "SmartX.SensorAttachments");
    }

    public async Task<AttachmentRecord> SaveFileAsync(
        int sensorId,
        IFormFile file)
    {
        if (sensorId <= 0)
        {
            throw new InvalidOperationException(
                "A valid sensor must be selected.");
        }

        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException(
                "Select a file to upload.");
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            throw new InvalidOperationException(
                "Files cannot be larger than 5 MB.");
        }

        string extension =
            Path.GetExtension(file.FileName);

        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                "This file type is not supported.");
        }

        Guid fileId = Guid.NewGuid();

        using var memoryStream = new MemoryStream();

        await file.CopyToAsync(memoryStream);

        byte[] encryptedData =
            protector.Protect(memoryStream.ToArray());

        string storageName = $"{fileId}.bin";

        string filePath =
            Path.Combine(uploadFolder, storageName);

        await File.WriteAllBytesAsync(
            filePath,
            encryptedData);

        var record = new AttachmentRecord
        {
            Id = fileId,
            SensorId = sensorId,
            FileName = Path.GetFileName(file.FileName),
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
            FileSize = file.Length,
            UploadedAt = DateTime.UtcNow
        };

        attachments.Add(record);

        return record;
    }

    public List<AttachmentRecord> GetSensorFiles(int sensorId)
    {
        return attachments
            .Where(file => file.SensorId == sensorId)
            .OrderByDescending(file => file.UploadedAt)
            .ToList();
    }

    public async Task<byte[]?> GetFileAsync(Guid id)
    {
        var record =
            attachments.FirstOrDefault(file => file.Id == id);

        if (record == null)
        {
            return null;
        }

        string filePath =
            Path.Combine(uploadFolder, $"{id}.bin");

        if (!File.Exists(filePath))
        {
            return null;
        }

        byte[] encryptedData =
            await File.ReadAllBytesAsync(filePath);

        return protector.Unprotect(encryptedData);
    }

    public AttachmentRecord? GetFileRecord(Guid id)
    {
        return attachments.FirstOrDefault(
            file => file.Id == id);
    }
}