
using SmartX.Shared;

namespace SmartX.Api.Services;

public class AlertRoutingService
{
    private readonly object sync = new();

    // Store alerts by importance and arrival order
    private readonly PriorityQueue<PriorityAlert, (int Level, long Order)>
        alertQueue = new();

    // Store connected network nodes
    private readonly Dictionary<string, List<NetworkConnection>> network =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Queue<AlertDispatch> dispatchHistory = new();

    private long arrivalOrder;
    private long dispatchedCount;

    public AlertRoutingService()
    {
        // Create a sample network with connection delays
        AddConnection("Hydroponics Bay", "Relay North", 3);
        AddConnection("Hydroponics Bay", "Gateway", 10);
        AddConnection("Relay North", "Gateway", 3);
        AddConnection("Relay North", "Control Hub", 12);

        AddConnection("Utility Room", "Gateway", 2);
        AddConnection("Utility Room", "Relay South", 4);

        AddConnection("Zone 2", "Relay South", 2);
        AddConnection("Zone 2", "Gateway", 9);

        AddConnection("Relay South", "Control Hub", 3);
        AddConnection("Gateway", "Control Hub", 4);
        AddConnection("Gateway", "Dashboard", 12);
        AddConnection("Control Hub", "Dashboard", 2);
    }

    // Check whether a network node exists
    public bool HasNode(string node)
    {
        lock (sync)
        {
            return network.ContainsKey(node);
        }
    }

    // Add an alert to the priority queue
    public AlertDashboardState SubmitAlert(
        AlertRequest request,
        bool dispatchCritical = true)
    {
        lock (sync)
        {
            string severity = NormalizeSeverity(request.Severity);

            var alert = new PriorityAlert
            {
                SensorId = request.SensorId,
                Message = request.Message.Trim(),
                SourceNode = request.SourceNode.Trim(),
                Severity = severity,
                CreatedUtc = DateTime.UtcNow
            };

            int priority = GetPriority(severity);

            arrivalOrder++;

            alertQueue.Enqueue(
                alert,
                (priority, arrivalOrder));

            // Send critical alerts before less important alerts
            if (dispatchCritical && priority == 0)
            {
                while (alertQueue.TryPeek(
                    out _,
                    out var nextPriority)
                    && nextPriority.Level == 0)
                {
                    DispatchOneInternal();
                }
            }

            return BuildDashboardState();
        }
    }

    // Send alerts waiting in the queue
    public AlertDashboardState DispatchNext(int count = 1)
    {
        lock (sync)
        {
            for (int i = 0; i < count && alertQueue.Count > 0; i++)
            {
                DispatchOneInternal();
            }

            return BuildDashboardState();
        }
    }

    public AlertDashboardState GetDashboardState()
    {
        lock (sync)
        {
            return BuildDashboardState();
        }
    }

    // Return the network and its connections
    public NetworkOverview GetNetwork()
    {
        lock (sync)
        {
            var connections = new List<NetworkConnection>();

            foreach (var node in network)
            {
                foreach (NetworkConnection connection in node.Value)
                {
                    if (string.Compare(
                        connection.FromNode,
                        connection.ToNode,
                        StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        connections.Add(new NetworkConnection
                        {
                            FromNode = connection.FromNode,
                            ToNode = connection.ToNode,
                            LatencyMs = connection.LatencyMs
                        });
                    }
                }
            }

            return new NetworkOverview
            {
                Nodes = network.Keys.OrderBy(x => x).ToList(),
                Connections = connections
                    .OrderBy(x => x.FromNode)
                    .ThenBy(x => x.ToNode)
                    .ToList()
            };
        }
    }

    public AlertRouteResult FindRoute(string sourceNode)
    {
        lock (sync)
        {
            return FindRouteInternal(sourceNode, "Dashboard");
        }
    }

    // Connect two nodes in both directions
    private void AddConnection(string from, string to, int latencyMs)
    {
        if (latencyMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(latencyMs));
        }

        if (!network.ContainsKey(from))
        {
            network[from] = new List<NetworkConnection>();
        }

        if (!network.ContainsKey(to))
        {
            network[to] = new List<NetworkConnection>();
        }

        network[from].Add(new NetworkConnection
        {
            FromNode = from,
            ToNode = to,
            LatencyMs = latencyMs
        });

        network[to].Add(new NetworkConnection
        {
            FromNode = to,
            ToNode = from,
            LatencyMs = latencyMs
        });
    }

    private static string NormalizeSeverity(string severity)
    {
        if (severity.Equals(
            "Critical",
            StringComparison.OrdinalIgnoreCase))
        {
            return "Critical";
        }

        if (severity.Equals(
            "High",
            StringComparison.OrdinalIgnoreCase))
        {
            return "High";
        }

        return "Normal";
    }

    // Lower numbers have higher priority
    private static int GetPriority(string severity)
    {
        return severity switch
        {
            "Critical" => 0,
            "High" => 1,
            _ => 2
        };
    }

    private void DispatchOneInternal()
    {
        if (alertQueue.Count == 0)
        {
            return;
        }

        PriorityAlert alert = alertQueue.Dequeue();

        AlertRouteResult route = FindRouteInternal(
            alert.SourceNode,
            "Dashboard");

        dispatchHistory.Enqueue(new AlertDispatch
        {
            Alert = alert,
            Route = route,
            DispatchedUtc = DateTime.UtcNow
        });

        dispatchedCount++;

        // Keep the latest 50 dispatch results
        if (dispatchHistory.Count > 50)
        {
            dispatchHistory.Dequeue();
        }
    }

    // Find the route with the lowest total delay
    private AlertRouteResult FindRouteInternal(
        string sourceNode,
        string destinationNode)
    {
        var result = new AlertRouteResult
        {
            SourceNode = sourceNode,
            DestinationNode = destinationNode,
            Status = "No route available"
        };

        if (!network.ContainsKey(sourceNode) ||
            !network.ContainsKey(destinationNode))
        {
            result.Status = "Network node not found";
            return result;
        }

        // Store the shortest known distance to each node
        var distances = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        var previous = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        var visited = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var nextNodes = new PriorityQueue<string, int>();

        foreach (string node in network.Keys)
        {
            distances[node] = int.MaxValue;
        }

        distances[sourceNode] = 0;
        nextNodes.Enqueue(sourceNode, 0);

        // Visit the nearest available node first
        while (nextNodes.Count > 0)
        {
            string current = nextNodes.Dequeue();

            if (!visited.Add(current))
            {
                continue;
            }

            if (current.Equals(
                destinationNode,
                StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            foreach (NetworkConnection connection in network[current])
            {
                if (visited.Contains(connection.ToNode))
                {
                    continue;
                }

                int newDistance =
                    distances[current] + connection.LatencyMs;

                // Update the route when a shorter path is found
                if (newDistance < distances[connection.ToNode])
                {
                    distances[connection.ToNode] = newDistance;
                    previous[connection.ToNode] = current;

                    nextNodes.Enqueue(
                        connection.ToNode,
                        newDistance);
                }
            }
        }

        if (distances[destinationNode] == int.MaxValue)
        {
            return result;
        }

        // Follow previous nodes to build the final route
        var path = new List<string>();

        string step = destinationNode;
        path.Add(step);

        while (!step.Equals(
            sourceNode,
            StringComparison.OrdinalIgnoreCase))
        {
            if (!previous.TryGetValue(step, out string? parent))
            {
                return result;
            }

            step = parent;
            path.Add(step);
        }

        path.Reverse();

        result.IsReachable = true;
        result.TotalLatencyMs = distances[destinationNode];
        result.Path = path;
        result.Status = "Delivered";

        return result;
    }

    private AlertDashboardState BuildDashboardState()
    {
        // Sort queued alerts for display
        var pending = alertQueue.UnorderedItems
            .OrderBy(item => item.Priority.Level)
            .ThenBy(item => item.Priority.Order)
            .Select(item => item.Element)
            .ToList();

        return new AlertDashboardState
        {
            PendingCount = pending.Count,

            CriticalPendingCount = pending.Count(
                alert => alert.Severity == "Critical"),

            DispatchedCount = dispatchedCount,

            PendingAlerts = pending,

            RecentDispatches = dispatchHistory
                .Reverse()
                .ToList()
        };
    }
}
