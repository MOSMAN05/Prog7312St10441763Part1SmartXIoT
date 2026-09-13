namespace SmartX.Shared;

public class DeploymentNode
{
    public string Name { get; set; } = string.Empty;

    public bool IsConfigured { get; set; } = true;

    public List<DeploymentNode> Children { get; set; } = new();
}