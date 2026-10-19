
using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/recommendations")]
public class RecommendationsController : ControllerBase
{
    private readonly RecommendationService recommendationService;

    public RecommendationsController(
        RecommendationService recommendationService)
    {
        this.recommendationService = recommendationService;
    }

    // Get the current recommendation data
    [HttpGet("overview")]
    public ActionResult<RecommendationOverview> GetOverview()
    {
        return Ok(recommendationService.GetOverview());
    }

    // Record a user search or selected command
    [HttpPost("activity")]
    public ActionResult<RecommendationOverview> RecordActivity(
        [FromBody] CommandActionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ActionName))
        {
            return BadRequest("An action name is required.");
        }

        if (request.ActivityType != "Search" &&
            request.ActivityType != "Command Selection")
        {
            return BadRequest("Invalid activity type.");
        }

        return Ok(recommendationService.RecordActivity(request));
    }

    // Predict the next action
    [HttpGet("predict")]
    public ActionResult<RecommendationResult> PredictNext(
        [FromQuery] string action)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            return BadRequest("Select an action first.");
        }

        return Ok(recommendationService.PredictNext(action));
    }

    // Load sample command history
    [HttpPost("demo")]
    public ActionResult<RecommendationOverview> LoadDemo()
    {
        return Ok(recommendationService.LoadDemoHistory());
    }
}

