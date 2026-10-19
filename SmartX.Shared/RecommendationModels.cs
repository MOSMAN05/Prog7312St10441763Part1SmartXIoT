
using System.ComponentModel.DataAnnotations;

namespace SmartX.Shared;

public class CommandActionRequest
{
    [Required]
    [StringLength(80)]
    public string ActionName { get; set; } = string.Empty;

    public string ActivityType { get; set; } = "Search";
}

public class CommandEdge
{
    public string TargetAction { get; set; } = string.Empty;

    public int Frequency { get; set; }
}

public class CommandActivity
{
    public string ActionName { get; set; } = string.Empty;

    public string ActivityType { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; }
}

public class TransitionRecord
{
    public string FromAction { get; set; } = string.Empty;

    public string ToAction { get; set; } = string.Empty;

    public int Frequency { get; set; }
}

public class RecommendationResult
{
    public string CurrentAction { get; set; } = string.Empty;

    public string SuggestedAction { get; set; } = "NO_PREDICTION";

    public double ConfidencePercent { get; set; }

    public int TotalObservations { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public class RecommendationOverview
{
    public int TrackedActions { get; set; }

    public int TotalTransitions { get; set; }

    public RecommendationResult CurrentRecommendation { get; set; } = new();

    public List<CommandActivity> RecentActivities { get; set; } = new();

    public List<TransitionRecord> Transitions { get; set; } = new();
}
