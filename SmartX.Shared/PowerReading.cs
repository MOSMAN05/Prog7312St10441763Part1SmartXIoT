namespace SmartX.Shared;

public class PowerReading
{
    public string MeterName { get; set; } = string.Empty;

    public double Kilowatts { get; set; }

    public static PowerReading operator +(PowerReading left, PowerReading right)
    {
        return new PowerReading
        {
            MeterName = $"{left.MeterName} + {right.MeterName}",
            Kilowatts = left.Kilowatts + right.Kilowatts
        };
    }

    public static bool operator >(PowerReading left, PowerReading right)
    {
        return left.Kilowatts > right.Kilowatts;
    }

    public static bool operator <(PowerReading left, PowerReading right)
    {
        return left.Kilowatts < right.Kilowatts;
    }
}