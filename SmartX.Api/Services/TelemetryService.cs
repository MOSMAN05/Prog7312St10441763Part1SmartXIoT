
using System.Globalization;
using SmartX.Shared;

namespace SmartX.Api.Services;

public class TelemetryService
{
    private readonly object sync = new();

    // Store incoming sensor readings
    private readonly Queue<TelemetryEvent> incomingQueue = new();

    // Keep telemetry logs ordered by time
    private readonly SortedDictionary<DateTime, List<TelemetryRecord>> indexedLogs = new();
    private readonly SortedSet<DateTime> timestampKeys = new();
    private readonly Queue<int> recentlyProcessedIds = new();

    private readonly TelemetryBuffer<float> temperaturePackets = new();
    private readonly TelemetryBuffer<int> powerPackets = new();
    private readonly TelemetryBuffer<bool> actuatorPackets = new();
    private readonly List<float[]> temperatureHistory = new();

    private int batchNumber;
    private long totalProcessed;
    private int anomalyCount;
    private double aggregatePowerKw;
    private string higherPowerSource = string.Empty;
    private DateTime lastUpdated;

    public TelemetryService()
    {
        SeedHistoricalTelemetry();
        GenerateBatch();
    }

    // Add new readings to the queue
    public TelemetryQueueStatus Enqueue(
        TelemetryEvent telemetry,
        bool processImmediately = true)
    {
        lock (sync)
        {
            EnqueueInternal(telemetry);

            if (processImmediately)
            {
                ProcessPendingInternal();
            }

            return GetQueueStatusInternal();
        }
    }

    // Process readings waiting in the queue
    public TelemetryQueueStatus ProcessPending()
    {
        lock (sync)
        {
            ProcessPendingInternal();
            return GetQueueStatusInternal();
        }
    }

    public TelemetryQueueStatus GetQueueStatus()
    {
        lock (sync)
        {
            return GetQueueStatusInternal();
        }
    }

    // Generate sample readings for the dashboard
    public TelemetrySummary GenerateBatch()
    {
        lock (sync)
        {
            batchNumber++;

            float temperature = batchNumber % 2 == 0
                ? 42.5f
                : Random.Shared.Next(220, 280) / 10f;

            float[] readings =
            {
                temperature,
                temperature + 0.2f,
                temperature + 0.1f
            };

            foreach (float reading in readings)
            {
                EnqueueInternal(new TelemetryEvent
                {
                    SensorId = 1,
                    DeviceName = "Greenhouse Temperature 01",
                    Metric = "Temperature",
                    Value = reading.ToString(
                        "0.0",
                        CultureInfo.InvariantCulture),
                    Unit = "C",
                    Timestamp = DateTime.UtcNow
                });
            }

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

            PowerReading total = meterOne + meterTwo;

            EnqueueInternal(new TelemetryEvent
            {
                SensorId = 2,
                DeviceName = "Smart Meter 01",
                Metric = "Power Load",
                Value = (meterOneWatts + meterTwoWatts)
                    .ToString(CultureInfo.InvariantCulture),
                Unit = "W",
                Timestamp = DateTime.UtcNow
            });

            EnqueueInternal(new TelemetryEvent
            {
                SensorId = 3,
                DeviceName = "Pump Controller 01",
                Metric = "Pump State",
                Value = batchNumber % 3 == 0 ? "Off" : "On",
                Timestamp = DateTime.UtcNow
            });

            ProcessPendingInternal();

            aggregatePowerKw = total.Kilowatts;

            higherPowerSource = meterOne > meterTwo
                ? meterOne.MeterName
                : meterTwo.MeterName;

            return GetSummaryInternal();
        }
    }

    // Get the most recent sensor readings
    public List<TelemetryRecord> GetRecentRecords(int count = 12)
    {
        lock (sync)
        {
            var records = new List<TelemetryRecord>();

            foreach (DateTime timestamp in timestampKeys.Reverse())
            {
                List<TelemetryRecord> group = indexedLogs[timestamp];

                for (int i = group.Count - 1;
                     i >= 0 && records.Count < count;
                     i--)
                {
                    records.Add(group[i]);
                }

                if (records.Count >= count)
                {
                    break;
                }
            }

            return records;
        }
    }

    // Find readings between two dates
    public List<TelemetryRecord> GetLogsByTime(
        DateTime fromUtc,
        DateTime toUtc,
        int limit = 200)
    {
        lock (sync)
        {
            DateTime start = NormalizeUtc(fromUtc);
            DateTime end = NormalizeUtc(toUtc);

            var result = new List<TelemetryRecord>();

            if (start > end || limit <= 0)
            {
                return result;
            }

            foreach (DateTime timestamp in
                     timestampKeys.GetViewBetween(start, end))
            {
                foreach (TelemetryRecord record in indexedLogs[timestamp])
                {
                    result.Add(record);

                    if (result.Count >= limit)
                    {
                        return result;
                    }
                }
            }

            return result;
        }
    }

    public TelemetrySummary GetSummary()
    {
        lock (sync)
        {
            return GetSummaryInternal();
        }
    }

    public float[][] GetTemperatureHistory()
    {
        lock (sync)
        {
            return temperatureHistory
                .Select(batch => batch.ToArray())
                .ToArray();
        }
    }

    private void EnqueueInternal(TelemetryEvent telemetry)
    {
        // Make a copy of the incoming reading
        incomingQueue.Enqueue(new TelemetryEvent
        {
            SensorId = telemetry.SensorId,
            DeviceName = telemetry.DeviceName,
            Metric = telemetry.Metric,
            Value = telemetry.Value,
            Unit = telemetry.Unit,
            IsAnomaly = telemetry.IsAnomaly,
            Timestamp = NormalizeUtc(telemetry.Timestamp)
        });
    }

    private void ProcessPendingInternal()
    {
        var processedTemperatures = new List<float>();

        // Process readings in the order received
        while (incomingQueue.Count > 0)
        {
            TelemetryEvent telemetry = incomingQueue.Dequeue();

            float? temperature = ProcessEventInternal(telemetry);

            if (temperature.HasValue)
            {
                processedTemperatures.Add(temperature.Value);
            }
        }

        if (processedTemperatures.Count > 0)
        {
            temperatureHistory.Add(
                processedTemperatures.ToArray());
        }
    }

    private float? ProcessEventInternal(TelemetryEvent telemetry)
    {
        bool isAnomaly = telemetry.IsAnomaly;
        float? processedTemperature = null;

        // Check temperature readings
        if (telemetry.Metric.Equals(
                "Temperature",
                StringComparison.OrdinalIgnoreCase)
            && float.TryParse(
                telemetry.Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float temperature))
        {
            isAnomaly |= temperature > 35;
            processedTemperature = temperature;

            temperaturePackets.Add(new TelemetryPacket<float>
            {
                SensorId = telemetry.SensorId,
                Value = temperature,
                Unit = telemetry.Unit,
                Timestamp = telemetry.Timestamp
            });
        }
        else if (telemetry.Metric.Equals(
                     "Power Load",
                     StringComparison.OrdinalIgnoreCase)
                 && int.TryParse(
                     telemetry.Value,
                     NumberStyles.Integer,
                     CultureInfo.InvariantCulture,
                     out int watts))
        {
            // Check power usage
            isAnomaly |= watts > 3000;
            aggregatePowerKw = watts / 1000.0;

            powerPackets.Add(new TelemetryPacket<int>
            {
                SensorId = telemetry.SensorId,
                Value = watts,
                Unit = telemetry.Unit,
                Timestamp = telemetry.Timestamp
            });

            if (string.IsNullOrEmpty(higherPowerSource))
            {
                higherPowerSource = "Device telemetry";
            }
        }
        else if (telemetry.Metric.Equals(
                     "Pump State",
                     StringComparison.OrdinalIgnoreCase))
        {
            // Check if the pump is on or off
            bool isOn = telemetry.Value.Equals(
                            "On",
                            StringComparison.OrdinalIgnoreCase)
                        || telemetry.Value.Equals(
                            "True",
                            StringComparison.OrdinalIgnoreCase);

            actuatorPackets.Add(new TelemetryPacket<bool>
            {
                SensorId = telemetry.SensorId,
                Value = isOn,
                Unit = telemetry.Unit,
                Timestamp = telemetry.Timestamp
            });
        }

        var record = new TelemetryRecord
        {
            SensorId = telemetry.SensorId,
            DeviceName = telemetry.DeviceName,
            Metric = telemetry.Metric,
            Value = telemetry.Value,
            Unit = telemetry.Unit,
            IsAnomaly = isAnomaly,
            Timestamp = telemetry.Timestamp
        };

        // Save the reading under its timestamp
        if (!indexedLogs.TryGetValue(
                record.Timestamp,
                out List<TelemetryRecord>? group))
        {
            group = new List<TelemetryRecord>();

            indexedLogs.Add(record.Timestamp, group);
            timestampKeys.Add(record.Timestamp);
        }

        group.Add(record);
        totalProcessed++;

        if (isAnomaly)
        {
            anomalyCount++;
        }

        recentlyProcessedIds.Enqueue(record.SensorId);

        if (recentlyProcessedIds.Count > 10)
        {
            recentlyProcessedIds.Dequeue();
        }

        lastUpdated = DateTime.UtcNow;

        return processedTemperature;
    }

    private TelemetryQueueStatus GetQueueStatusInternal()
    {
        return new TelemetryQueueStatus
        {
            PendingCount = incomingQueue.Count,
            TotalProcessed = totalProcessed,
            IndexedTimestampCount = timestampKeys.Count,

            OldestPendingTimestamp = incomingQueue.Count == 0
                ? null
                : incomingQueue.Peek().Timestamp,

            RecentlyProcessedSensorIds =
                recentlyProcessedIds.ToList()
        };
    }

    private TelemetrySummary GetSummaryInternal()
    {
        var temperature = temperaturePackets.GetLatest();
        var power = powerPackets.GetLatest();
        var actuator = actuatorPackets.GetLatest();

        return new TelemetrySummary
        {
            TotalPackets = (int)Math.Min(
                totalProcessed,
                int.MaxValue),

            AnomalyCount = anomalyCount,
            LatestTemperatureC = temperature?.Value ?? 0,
            LatestPowerWatts = power?.Value ?? 0,
            LatestActuatorState = actuator?.Value ?? false,
            AggregatePowerKw = aggregatePowerKw,
            HigherPowerSource = higherPowerSource,
            LastUpdated = lastUpdated
        };
    }

    // Add sample temperature readings
    private void SeedHistoricalTelemetry()
    {
        float[][] history =
        {
            new[] { 22.1f, 22.4f, 22.8f },
            new[] { 23.0f, 23.2f, 23.1f },
            new[] { 23.4f, 23.6f, 23.8f }
        };

        DateTime timestamp = DateTime.UtcNow.AddMinutes(-15);

        foreach (float[] batch in history)
        {
            foreach (float temperature in batch)
            {
                EnqueueInternal(new TelemetryEvent
                {
                    SensorId = 1,
                    DeviceName = "Greenhouse Temperature 01",
                    Metric = "Temperature",

                    Value = temperature.ToString(
                        "0.0",
                        CultureInfo.InvariantCulture),

                    Unit = "C",
                    Timestamp = timestamp
                });

                timestamp = timestamp.AddMinutes(1);
            }

            ProcessPendingInternal();
        }
    }

    private static DateTime NormalizeUtc(DateTime timestamp)
    {
        return timestamp.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(
                timestamp,
                DateTimeKind.Utc)
            : timestamp.ToUniversalTime();
    }
}
