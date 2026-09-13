using SmartX.Shared;

namespace SmartX.Api.Services;

public class TelemetryService
{
    private readonly TelemetryBuffer<float> temperaturePackets = new();
    private readonly TelemetryBuffer<int> powerPackets = new();
    private readonly TelemetryBuffer<bool> actuatorPackets = new();

    private readonly List<TelemetryRecord> telemetryRecords = new();

    private float[][] temperatureHistory =
    {
        new float[] { 22.1f, 22.4f, 22.8f },
        new float[] { 23.0f, 23.2f, 23.1f },
        new float[] { 23.4f, 23.6f, 23.8f }
    };

    private int batchNumber;
    private double aggregatePowerKw;
    private string higherPowerSource = string.Empty;

    public TelemetryService()
    {
        SeedHistoricalTelemetry();
        GenerateBatch();
    }

    public TelemetrySummary GenerateBatch()
    {
        batchNumber++;

        bool createTemperatureSpike = batchNumber % 2 == 0;

        float temperature = createTemperatureSpike
            ? 42.5f
            : Random.Shared.Next(220, 280) / 10f;

        float[] temperatureBatch =
        {
            temperature,
            temperature + 0.2f,
            temperature + 0.1f
        };

        temperatureHistory = temperatureHistory
            .Append(temperatureBatch)
            .ToArray();

        foreach (float value in temperatureBatch)
        {
            var packet = new TelemetryPacket<float>
            {
                SensorId = 1,
                Value = value,
                Unit = "C",
                Timestamp = DateTime.UtcNow
            };

            temperaturePackets.Add(packet);

            telemetryRecords.Add(new TelemetryRecord
            {
                SensorId = 1,
                DeviceName = "Greenhouse Temperature 01",
                Metric = "Temperature",
                Value = value.ToString("0.0"),
                Unit = "C",
                IsAnomaly = value > 35,
                Timestamp = packet.Timestamp
            });
        }

        AddPowerTelemetry();
        AddActuatorTelemetry();

        return GetSummary();
    }

    public List<TelemetryRecord> GetRecentRecords(int count = 12)
    {
        return telemetryRecords
            .OrderByDescending(record => record.Timestamp)
            .Take(count)
            .ToList();
    }

    public TelemetrySummary GetSummary()
    {
        var latestTemperature = temperaturePackets.GetLatest();
        var latestPower = powerPackets.GetLatest();
        var latestActuator = actuatorPackets.GetLatest();

        return new TelemetrySummary
        {
            TotalPackets =
                temperaturePackets.Count +
                powerPackets.Count +
                actuatorPackets.Count,

            AnomalyCount = telemetryRecords.Count(record => record.IsAnomaly),

            LatestTemperatureC =
                latestTemperature == null ? 0 : latestTemperature.Value,

            LatestPowerWatts =
                latestPower == null ? 0 : latestPower.Value,

            LatestActuatorState =
                latestActuator != null && latestActuator.Value,

            AggregatePowerKw = aggregatePowerKw,

            HigherPowerSource = higherPowerSource,

            LastUpdated = DateTime.UtcNow
        };
    }

    public float[][] GetTemperatureHistory()
    {
        return temperatureHistory;
    }

    private void AddPowerTelemetry()
    {
        int meterOneWatts = Random.Shared.Next(900, 1700);
        int meterTwoWatts = Random.Shared.Next(700, 1500);

        var meterOne = new PowerReading
        {
            MeterName = "Meter A",
            Kilowatts = meterOneWatts / 1000.0
        };

        var meterTwo = new PowerReading
        {
            MeterName = "Meter B",
            Kilowatts = meterTwoWatts / 1000.0
        };

        PowerReading combinedLoad = meterOne + meterTwo;

        aggregatePowerKw = combinedLoad.Kilowatts;

        higherPowerSource = meterOne > meterTwo
            ? meterOne.MeterName
            : meterTwo.MeterName;

        int totalWatts = meterOneWatts + meterTwoWatts;

        var packet = new TelemetryPacket<int>
        {
            SensorId = 2,
            Value = totalWatts,
            Unit = "W",
            Timestamp = DateTime.UtcNow
        };

        powerPackets.Add(packet);

        telemetryRecords.Add(new TelemetryRecord
        {
            SensorId = 2,
            DeviceName = "Smart Meter 01",
            Metric = "Power Load",
            Value = totalWatts.ToString(),
            Unit = "W",
            IsAnomaly = totalWatts > 3000,
            Timestamp = packet.Timestamp
        });
    }

    private void AddActuatorTelemetry()
    {
        bool state = batchNumber % 3 != 0;

        var packet = new TelemetryPacket<bool>
        {
            SensorId = 3,
            Value = state,
            Unit = "State",
            Timestamp = DateTime.UtcNow
        };

        actuatorPackets.Add(packet);

        telemetryRecords.Add(new TelemetryRecord
        {
            SensorId = 3,
            DeviceName = "Pump Controller 01",
            Metric = "Pump State",
            Value = state ? "On" : "Off",
            Unit = string.Empty,
            IsAnomaly = false,
            Timestamp = packet.Timestamp
        });
    }

    private void SeedHistoricalTelemetry()
    {
        DateTime timestamp = DateTime.UtcNow.AddMinutes(-15);

        foreach (float[] batch in temperatureHistory)
        {
            foreach (float value in batch)
            {
                var packet = new TelemetryPacket<float>
                {
                    SensorId = 1,
                    Value = value,
                    Unit = "C",
                    Timestamp = timestamp
                };

                temperaturePackets.Add(packet);

                telemetryRecords.Add(new TelemetryRecord
                {
                    SensorId = 1,
                    DeviceName = "Greenhouse Temperature 01",
                    Metric = "Temperature",
                    Value = value.ToString("0.0"),
                    Unit = "C",
                    IsAnomaly = value > 35,
                    Timestamp = timestamp
                });

                timestamp = timestamp.AddMinutes(1);
            }
        }
    }
}