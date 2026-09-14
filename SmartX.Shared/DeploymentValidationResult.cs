namespace SmartX.Shared;

public class DeploymentValidationResult
{
    public bool IsValid { get; set; }

    public int NodesChecked { get; set; }

    public List<string> Issues { get; set; } = new();
}
