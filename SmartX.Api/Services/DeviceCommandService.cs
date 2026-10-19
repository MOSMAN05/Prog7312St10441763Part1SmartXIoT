
using SmartX.Shared;

namespace SmartX.Api.Services;

public class DeviceCommandService
{
    private readonly object sync = new();
    private readonly SensorRegistryService registry;

    private readonly Dictionary<int, bool> actuatorStates = new();

    // Store previous commands for undo
    private readonly Stack<CommandHistoryEntry> undoStack = new();

    private readonly Queue<CommandHistoryEntry> commandHistory = new();

    private long totalCommands;

    public DeviceCommandService(SensorRegistryService registry)
    {
        this.registry = registry;
    }

    public CommandDashboard GetDashboard()
    {
        lock (sync)
        {
            return BuildDashboard();
        }
    }

    public CommandOperationResult Execute(DeviceCommandRequest request)
    {
        lock (sync)
        {
            Sensor? sensor = registry.GetById(request.SensorId);

            if (sensor == null)
            {
                return Failed("Sensor not found.");
            }

            if (sensor.Category != SensorCategory.Actuator)
            {
                return Failed("This device does not support manual commands.");
            }

            if (!sensor.IsOnline)
            {
                return Failed("Reconnect the device before sending commands.");
            }

            bool previousState = actuatorStates.TryGetValue(
                sensor.Id,
                out bool currentState) && currentState;

            if (previousState == request.TurnOn)
            {
                return Failed("The device is already in that state.");
            }

            string action;

            if (sensor.DeviceName.Contains(
                "Pump",
                StringComparison.OrdinalIgnoreCase))
            {
                action = request.TurnOn
                    ? "Turn Pump On"
                    : "Turn Pump Off";
            }
            else
            {
                action = request.TurnOn
                    ? $"Turn {sensor.DeviceName} On"
                    : $"Turn {sensor.DeviceName} Off";
            }

            actuatorStates[sensor.Id] = request.TurnOn;

            var command = new CommandHistoryEntry
            {
                SensorId = sensor.Id,
                DeviceName = sensor.DeviceName,
                Action = action,
                PreviousState = previousState,
                NewState = request.TurnOn,
                IsUndo = false,
                TimestampUtc = DateTime.UtcNow
            };

            undoStack.Push(command);

            SaveHistory(command);
            totalCommands++;

            return new CommandOperationResult
            {
                Success = true,
                Message = "Command completed successfully.",
                ActionName = action,
                Dashboard = BuildDashboard()
            };
        }
    }

    public CommandOperationResult UndoLast()
    {
        lock (sync)
        {
            if (undoStack.Count == 0)
            {
                return Failed("There are no commands to undo.");
            }

            CommandHistoryEntry lastCommand = undoStack.Peek();

            Sensor? sensor = registry.GetById(lastCommand.SensorId);

            if (sensor == null || !sensor.IsOnline)
            {
                return Failed(
                    "The device must be online before undoing its command.");
            }

            // Restore the state before the last command
            undoStack.Pop();

            actuatorStates[lastCommand.SensorId] =
                lastCommand.PreviousState;

            var undoEntry = new CommandHistoryEntry
            {
                SensorId = lastCommand.SensorId,
                DeviceName = lastCommand.DeviceName,
                Action = $"Undo: {lastCommand.Action}",
                PreviousState = lastCommand.NewState,
                NewState = lastCommand.PreviousState,
                IsUndo = true,
                TimestampUtc = DateTime.UtcNow
            };

            SaveHistory(undoEntry);
            totalCommands++;

            return new CommandOperationResult
            {
                Success = true,
                Message = "The last command was undone.",
                ActionName = "Undo Last Command",
                Dashboard = BuildDashboard()
            };
        }
    }

    private void SaveHistory(CommandHistoryEntry command)
    {
        commandHistory.Enqueue(command);

        if (commandHistory.Count > 100)
        {
            commandHistory.Dequeue();
        }
    }

    private CommandOperationResult Failed(string message)
    {
        return new CommandOperationResult
        {
            Success = false,
            Message = message,
            Dashboard = BuildDashboard()
        };
    }

    private CommandDashboard BuildDashboard()
    {
        List<ActuatorState> actuators = registry.GetAll()
            .Where(sensor => sensor.Category == SensorCategory.Actuator)
            .Select(sensor => new ActuatorState
            {
                SensorId = sensor.Id,
                DeviceName = sensor.DeviceName,
                IsOnline = sensor.IsOnline,
                IsOn = actuatorStates.TryGetValue(
                    sensor.Id,
                    out bool state) && state
            })
            .ToList();

        return new CommandDashboard
        {
            TotalCommands = totalCommands,
            UndoAvailable = undoStack.Count,
            Actuators = actuators,
            History = commandHistory.Reverse().ToList()
        };
    }
}
