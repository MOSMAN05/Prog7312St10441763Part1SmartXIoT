using System.ComponentModel.DataAnnotations;

namespace SmartX.Shared;

public class Sensor
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Device name is required.")]
    public string DeviceName { get; set; } = string.Empty;

    [Required(ErrorMessage = "MAC address is required.")]
    [RegularExpression(
        "^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$",
        ErrorMessage = "Enter a valid MAC address.")]
    public string MacAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Deployment location is required.")]
    public string DeploymentLocation { get; set; } = string.Empty;

    public SensorCategory Category { get; set; }

    public bool IsOnline { get; set; } = true;

    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    public DateTime DateRegistered { get; set; } = DateTime.UtcNow;
}