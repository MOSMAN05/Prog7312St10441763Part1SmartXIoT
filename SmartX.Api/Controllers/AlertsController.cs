
using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly AlertRoutingService alertService;

    public AlertsController(AlertRoutingService alertService)
    {
        this.alertService = alertService;
    }

    // Get the current alert queue and history
    [HttpGet]
    public ActionResult<AlertDashboardState> GetAlerts()
    {
        return Ok(alertService.GetDashboardState());
    }

    // Add a new alert from a simulated device
    [HttpPost]
    public ActionResult<AlertDashboardState> SubmitAlert(
        [FromBody] AlertRequest request,
        [FromQuery] bool dispatchCritical = true)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Enter an alert message.");
        }

        if (string.IsNullOrWhiteSpace(request.SourceNode) ||
            !alertService.HasNode(request.SourceNode.Trim()))
        {
            return BadRequest("Select a valid network node.");
        }

        if (!new[] { "Normal", "High", "Critical" }
            .Contains(request.Severity, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest("Select a valid alert severity.");
        }

        return Ok(alertService.SubmitAlert(
            request,
            dispatchCritical));
    }

    // Dispatch alerts in priority order
    [HttpPost("dispatch")]
    public ActionResult<AlertDashboardState> DispatchAlerts(
        [FromQuery] int count = 1)
    {
        if (count < 1 || count > 20)
        {
            return BadRequest("Count must be between 1 and 20.");
        }

        return Ok(alertService.DispatchNext(count));
    }

    // Get the simulated network connections
    [HttpGet("network")]
    public ActionResult<NetworkOverview> GetNetwork()
    {
        return Ok(alertService.GetNetwork());
    }

    // Find the shortest route to the dashboard
    [HttpGet("route")]
    public ActionResult<AlertRouteResult> GetRoute(
        [FromQuery] string sourceNode)
    {
        if (string.IsNullOrWhiteSpace(sourceNode))
        {
            return BadRequest("A starting node is required.");
        }

        return Ok(alertService.FindRoute(sourceNode.Trim()));
    }
}
