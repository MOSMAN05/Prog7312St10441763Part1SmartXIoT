
using System.ComponentModel.DataAnnotations;

namespace SmartX.Shared;

public class AlertRequest
{
    [Range(1, int.MaxValue)]
    public int SensorId { get; set; } = 1;

    [Required]
    [StringLength(180)]
    public string Message { get; set; } = string.Empty;

    [Required]
    public string SourceNode { get; set; } = "Hydroponics Bay";

    [Required]
    public string Severity { get; set; } = "Normal";
}

public class PriorityAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int SensorId { get; set; }

    public string Message { get; set; } = string.Empty;

    public string SourceNode { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class NetworkConnection
{
    public string FromNode { get; set; } = string.Empty;

    public string ToNode { get; set; } = string.Empty;

    public int LatencyMs { get; set; }
}

public class NetworkOverview
{
    public List<string> Nodes { get; set; } = new();

    public List<NetworkConnection> Connections { get; set; } = new();
}

public class AlertRouteResult
{
    public string SourceNode { get; set; } = string.Empty;

    public string DestinationNode { get; set; } = "Dashboard";

    public bool IsReachable { get; set; }

    public int? TotalLatencyMs { get; set; }

    public List<string> Path { get; set; } = new();

    public string Status { get; set; } = string.Empty;
}

public class AlertDispatch
{
    public PriorityAlert Alert { get; set; } = new();

    public AlertRouteResult Route { get; set; } = new();

    public DateTime DispatchedUtc { get; set; }
}

public class AlertDashboardState
{
    public int PendingCount { get; set; }

    public int CriticalPendingCount { get; set; }

    public long DispatchedCount { get; set; }

    public List<PriorityAlert> PendingAlerts { get; set; } = new();

    public List<AlertDispatch> RecentDispatches { get; set; } = new();
}
