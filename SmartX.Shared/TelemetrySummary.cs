namespace SmartX.Shared;

public class TelemetrySummary
{
    public int TotalPackets { get; set; }

    public int AnomalyCount { get; set; }

    public float LatestTemperatureC { get; set; }

    public int LatestPowerWatts { get; set; }

    public bool LatestActuatorState { get; set; }

    public double AggregatePowerKw { get; set; }

    public string HigherPowerSource { get; set; } = string.Empty;

    public DateTime LastUpdated { get; set; }
}