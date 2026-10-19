
using SmartX.Shared;

namespace SmartX.Api.Services;

public class SensorRegistryService
{
    private readonly object sync = new();

    // Find sensors quickly using their MAC address
    private readonly Dictionary<string, Sensor> sensorsByMac =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<int, string> macById = new();

    // Keep track of sensors with an active disconnection warning
    private readonly HashSet<int> disconnectedSensors = new();

    private int nextId = 4;

    public SensorRegistryService()
    {
        // Load the original registered sensors
        AddInitialSensor(new Sensor
        {
            Id = 1,
            DeviceName = "Greenhouse Temperature 01",
            MacAddress = "A4:C1:38:22:0F:91",
            DeploymentLocation = "Facility A - Hydroponics Bay",
            Category = SensorCategory.Environmental,
            IsOnline = true,
            LastSeen = DateTime.UtcNow
        });

        AddInitialSensor(new Sensor
        {
            Id = 2,
            DeviceName = "Smart Meter 01",
            MacAddress = "B8:27:EB:44:21:10",
            DeploymentLocation = "Facility A - Utility Room",
            Category = SensorCategory.PowerConsumption,
            IsOnline = true,
            LastSeen = DateTime.UtcNow
        });

        AddInitialSensor(new Sensor
        {
            Id = 3,
            DeviceName = "Pump Controller 01",
            MacAddress = "C2:11:45:67:80:AB",
            DeploymentLocation = "Facility A - Zone 2",
            Category = SensorCategory.Actuator,
            IsOnline = false,
            LastSeen = DateTime.UtcNow.AddMinutes(-5)
        });
    }

    // Add a sensor to both dictionaries
    private void AddInitialSensor(Sensor sensor)
    {
        string mac = FormatMac(sensor.MacAddress);

        sensor.MacAddress = mac;

        sensorsByMac.Add(mac, sensor);
        macById.Add(sensor.Id, mac);

        if (!sensor.IsOnline)
        {
            disconnectedSensors.Add(sensor.Id);
        }
    }

    // Use the same format for all MAC addresses
    private static string FormatMac(string mac)
    {
        return mac.Trim()
            .Replace('-', ':')
            .ToUpperInvariant();
    }

    // Return a copy so the stored sensor cannot be changed directly
    private static Sensor CopySensor(Sensor sensor)
    {
        return new Sensor
        {
            Id = sensor.Id,
            DeviceName = sensor.DeviceName,
            MacAddress = sensor.MacAddress,
            DeploymentLocation = sensor.DeploymentLocation,
            Category = sensor.Category,
            IsOnline = sensor.IsOnline,
            LastSeen = sensor.LastSeen,
            DateRegistered = sensor.DateRegistered
        };
    }

    public List<Sensor> GetAll()
    {
        lock (sync)
        {
            return sensorsByMac.Values
                .OrderBy(sensor => sensor.Id)
                .Select(CopySensor)
                .ToList();
        }
    }

    // Find a sensor using its ID
    public Sensor? GetById(int id)
    {
        lock (sync)
        {
            if (!macById.TryGetValue(id, out string? mac))
            {
                return null;
            }

            return CopySensor(sensorsByMac[mac]);
        }
    }

    // Find a sensor using its MAC address
    public Sensor? GetByMac(string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return null;
        }

        lock (sync)
        {
            string mac = FormatMac(macAddress);

            return sensorsByMac.TryGetValue(mac, out Sensor? sensor)
                ? CopySensor(sensor)
                : null;
        }
    }

    // Register a new sensor
    public bool TryRegister(Sensor input, out Sensor? registered)
    {
        lock (sync)
        {
            string mac = FormatMac(input.MacAddress);

            if (sensorsByMac.ContainsKey(mac))
            {
                registered = null;
                return false;
            }

            DateTime now = DateTime.UtcNow;

            var sensor = new Sensor
            {
                Id = nextId++,
                DeviceName = input.DeviceName.Trim(),
                MacAddress = mac,
                DeploymentLocation = input.DeploymentLocation.Trim(),
                Category = input.Category,
                IsOnline = true,
                LastSeen = now,
                DateRegistered = now
            };

            sensorsByMac.Add(mac, sensor);
            macById.Add(sensor.Id, mac);

            registered = CopySensor(sensor);
            return true;
        }
    }

    // Update whether a sensor is connected
    public SensorConnectionResult? SetConnection(
        int id,
        bool isOnline)
    {
        lock (sync)
        {
            if (!macById.TryGetValue(id, out string? mac))
            {
                return null;
            }

            Sensor sensor = sensorsByMac[mac];
            bool newAlert = false;

            if (isOnline)
            {
                sensor.IsOnline = true;
                sensor.LastSeen = DateTime.UtcNow;

                // Remove the warning when the sensor reconnects
                disconnectedSensors.Remove(id);
            }
            else
            {
                sensor.IsOnline = false;

                // The set prevents repeated offline warnings
                newAlert = disconnectedSensors.Add(id);
            }

            return new SensorConnectionResult
            {
                Sensor = CopySensor(sensor),
                NewDisconnectedAlert = newAlert,
                ActiveDisconnectedAlerts = disconnectedSensors.Count
            };
        }
    }

    public DeviceRegistryStatus GetStatus()
    {
        lock (sync)
        {
            int online = sensorsByMac.Values.Count(s => s.IsOnline);

            return new DeviceRegistryStatus
            {
                RegisteredCount = sensorsByMac.Count,
                OnlineCount = online,
                DisconnectedCount = sensorsByMac.Count - online,
                TrackedOfflineAlerts = disconnectedSensors.Count
            };
        }
    }
}
