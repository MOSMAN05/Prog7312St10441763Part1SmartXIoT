
using SmartX.Shared;

namespace SmartX.Api.Services;

public class RecommendationService
{
    private readonly object sync = new();

    // Store commands and their related actions
    private readonly Dictionary<string, List<CommandEdge>> commandGraph =
        new(StringComparer.OrdinalIgnoreCase);

    // Keep recent user activity
    private readonly Queue<CommandActivity> recentActivities = new();

    private string? previousAction;
    private int totalTransitions;

    // Save a new search or command activity
    public RecommendationOverview RecordActivity(CommandActionRequest request)
    {
        lock (sync)
        {
            AddActivity(request.ActionName.Trim(), request.ActivityType);

            return BuildOverview();
        }
    }

    // Find the most likely next action
    public RecommendationResult PredictNext(string currentAction)
    {
        lock (sync)
        {
            return PredictNextInternal(currentAction.Trim());
        }
    }

    public RecommendationOverview GetOverview()
    {
        lock (sync)
        {
            return BuildOverview();
        }
    }

    // Add sample activity for testing
    public RecommendationOverview LoadDemoHistory()
    {
        lock (sync)
        {
            string? savedAction = previousAction;

            string[][] examples =
            {
                new[]
                {
                    "Search Temperature",
                    "Inspect Moisture",
                    "Turn Pump On"
                },
                new[]
                {
                    "Search Temperature",
                    "Inspect Moisture",
                    "Turn Pump On"
                },
                new[]
                {
                    "Search Temperature",
                    "Inspect Moisture",
                    "Turn Pump On"
                },
                new[]
                {
                    "Check Power",
                    "Inspect Alerts",
                    "Check Device"
                }
            };

            foreach (string[] sequence in examples)
            {
                previousAction = null;

                foreach (string action in sequence)
                {
                    AddActivity(action, "Demo");
                }
            }

            previousAction = savedAction;

            return BuildOverview();
        }
    }

    private void AddActivity(string actionName, string activityType)
    {
        // Add the action if it is not in the graph
        if (!commandGraph.ContainsKey(actionName))
        {
            commandGraph[actionName] = new List<CommandEdge>();
        }

        // Count how often one action follows another
        if (!string.IsNullOrEmpty(previousAction))
        {
            List<CommandEdge> edges = commandGraph[previousAction];

            CommandEdge? existingEdge = edges.FirstOrDefault(
                edge => edge.TargetAction.Equals(
                    actionName,
                    StringComparison.OrdinalIgnoreCase));

            if (existingEdge == null)
            {
                edges.Add(new CommandEdge
                {
                    TargetAction = actionName,
                    Frequency = 1
                });
            }
            else
            {
                existingEdge.Frequency++;
            }

            totalTransitions++;
        }

        previousAction = actionName;

        recentActivities.Enqueue(new CommandActivity
        {
            ActionName = actionName,
            ActivityType = activityType,
            TimestampUtc = DateTime.UtcNow
        });

        // Keep the latest 50 activities
        if (recentActivities.Count > 50)
        {
            recentActivities.Dequeue();
        }
    }

    private RecommendationResult PredictNextInternal(string currentAction)
    {
        var result = new RecommendationResult
        {
            CurrentAction = currentAction,
            SuggestedAction = "NO_PREDICTION",
            Reason = "Not enough activity history to make a prediction."
        };

        // Return safely when there is no matching history
        if (!commandGraph.TryGetValue(
                currentAction,
                out List<CommandEdge>? edges)
            || edges.Count == 0)
        {
            return result;
        }

        // Find the action with the highest frequency
        CommandEdge mostLikely = edges
            .OrderByDescending(edge => edge.Frequency)
            .ThenBy(edge => edge.TargetAction)
            .First();

        int total = edges.Sum(edge => edge.Frequency);

        result.SuggestedAction = mostLikely.TargetAction;
        result.TotalObservations = total;

        result.ConfidencePercent = Math.Round(
            (double)mostLikely.Frequency / total * 100,
            1);

        result.Reason =
            $"This action followed '{currentAction}' " +
            $"{mostLikely.Frequency} out of {total} times.";

        return result;
    }

    private RecommendationOverview BuildOverview()
    {
        var transitions = new List<TransitionRecord>();

        // Build a list of recorded command transitions
        foreach (var pair in commandGraph)
        {
            foreach (CommandEdge edge in pair.Value)
            {
                transitions.Add(new TransitionRecord
                {
                    FromAction = pair.Key,
                    ToAction = edge.TargetAction,
                    Frequency = edge.Frequency
                });
            }
        }

        transitions = transitions
            .OrderByDescending(item => item.Frequency)
            .ThenBy(item => item.FromAction)
            .ToList();

        return new RecommendationOverview
        {
            TrackedActions = commandGraph.Count,
            TotalTransitions = totalTransitions,

            CurrentRecommendation = PredictNextInternal(
                previousAction ?? string.Empty),

            RecentActivities = recentActivities
                .Reverse()
                .ToList(),

            Transitions = transitions
        };
    }
}
