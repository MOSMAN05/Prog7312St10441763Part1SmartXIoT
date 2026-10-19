
using System.ComponentModel.DataAnnotations;

namespace SmartX.Shared;

public class SensorConnectionRequest
{
    public bool IsOnline { get; set; }
}

public class SensorConnectionResult
{
    public Sensor Sensor { get; set; } = new();

    public bool NewDisconnectedAlert { get; set; }

    public int ActiveDisconnectedAlerts { get; set; }
}

public class DeviceRegistryStatus
{
    public int RegisteredCount { get; set; }

    public int OnlineCount { get; set; }

    public int DisconnectedCount { get; set; }

    public int TrackedOfflineAlerts { get; set; }
}

public class DeviceCommandRequest
{
    [Range(1, int.MaxValue)]
    public int SensorId { get; set; }

    public bool TurnOn { get; set; }
}

public class ActuatorState
{
    public int SensorId { get; set; }

    public string DeviceName { get; set; } = string.Empty;

    public bool IsOnline { get; set; }

    public bool IsOn { get; set; }
}

public class CommandHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int SensorId { get; set; }

    public string DeviceName { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public bool PreviousState { get; set; }

    public bool NewState { get; set; }

    public bool IsUndo { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

public class CommandDashboard
{
    public long TotalCommands { get; set; }

    public int UndoAvailable { get; set; }

    public List<ActuatorState> Actuators { get; set; } = new();

    public List<CommandHistoryEntry> History { get; set; } = new();
}

public class CommandOperationResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string ActionName { get; set; } = string.Empty;

    public CommandDashboard Dashboard { get; set; } = new();
}
