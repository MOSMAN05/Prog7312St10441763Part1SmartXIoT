using SmartX.Shared;

namespace SmartX.Api.Services;

public class DeploymentService
{
    private readonly DeploymentNode rootNode;

    public DeploymentService()
    {
        rootNode = new DeploymentNode
        {
            Name = "Facility A",
            IsConfigured = true,
            Children = new List<DeploymentNode>
            {
                new DeploymentNode
                {
                    Name = "Zone 1",
                    IsConfigured = true,
                    Children = new List<DeploymentNode>
                    {
                        new DeploymentNode
                        {
                            Name = "Hydroponics Bay",
                            IsConfigured = true
                        },
                        new DeploymentNode
                        {
                            Name = "Sub-Zone B",
                            IsConfigured = true
                        }
                    }
                },
                new DeploymentNode
                {
                    Name = "Zone 2",
                    IsConfigured = true,
                    Children = new List<DeploymentNode>
                    {
                        new DeploymentNode
                        {
                            Name = "Pump Room",
                            IsConfigured = false
                        },
                        new DeploymentNode
                        {
                            Name = "Utility Room",
                            IsConfigured = true
                        }
                    }
                }
            }
        };
    }

    public DeploymentNode GetDeploymentTree()
    {
        return rootNode;
    }

    public DeploymentValidationResult ValidateDeployment()
    {
        var issues = new List<string>();
        int nodesChecked = 0;

        ValidateNode(rootNode, issues, ref nodesChecked);

        return new DeploymentValidationResult
        {
            IsValid = issues.Count == 0,
            NodesChecked = nodesChecked,
            Issues = issues
        };
    }

    private void ValidateNode(
        DeploymentNode node,
        List<string> issues,
        ref int nodesChecked)
    {
        nodesChecked++;

        if (!node.IsConfigured)
        {
            issues.Add($"{node.Name} is not fully configured.");
        }

        foreach (var child in node.Children)
        {
            ValidateNode(child, issues, ref nodesChecked);
        }
    }
}