namespace SmartX.Shared;

public class TelemetryBuffer<T>
{
    private readonly List<TelemetryPacket<T>> packets = new();

    public int Count => packets.Count;

    public void Add(TelemetryPacket<T> packet)
    {
        packets.Add(packet);
    }

    public TelemetryPacket<T>? GetLatest()
    {
        return packets.LastOrDefault();
    }

    public List<TelemetryPacket<T>> GetRecent(int count)
    {
        return packets.TakeLast(count).ToList();
    }
}