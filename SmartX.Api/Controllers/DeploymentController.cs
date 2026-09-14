using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeploymentController : ControllerBase
{
    private readonly DeploymentService deploymentService;

    public DeploymentController(
        DeploymentService deploymentService)
    {
        this.deploymentService = deploymentService;
    }

    [HttpGet("tree")]
    public ActionResult<DeploymentNode> GetTree()
    {
        return Ok(deploymentService.GetDeploymentTree());
    }

    [HttpGet("validate")]
    public ActionResult<DeploymentValidationResult> Validate()
    {
        return Ok(
            deploymentService.ValidateDeployment());
    }
}