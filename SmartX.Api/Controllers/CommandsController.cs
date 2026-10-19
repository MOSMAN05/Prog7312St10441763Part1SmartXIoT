
using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/commands")]
public class CommandsController : ControllerBase
{
    private readonly DeviceCommandService commandService;
    private readonly RecommendationService recommendationService;

    public CommandsController(
        DeviceCommandService commandService,
        RecommendationService recommendationService)
    {
        this.commandService = commandService;
        this.recommendationService = recommendationService;
    }

    [HttpGet]
    public ActionResult<CommandDashboard> GetCommands()
    {
        return Ok(commandService.GetDashboard());
    }

    [HttpPost]
    public ActionResult<CommandOperationResult> SendCommand(
        [FromBody] DeviceCommandRequest request)
    {
        CommandOperationResult result = commandService.Execute(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        // Save successful commands for future predictions
        recommendationService.RecordActivity(
            new CommandActionRequest
            {
                ActionName = result.ActionName,
                ActivityType = "Command Selection"
            });

        return Ok(result);
    }

    [HttpPost("undo")]
    public ActionResult<CommandOperationResult> UndoLastCommand()
    {
        CommandOperationResult result = commandService.UndoLast();

        if (!result.Success)
        {
            return BadRequest(result);
        }

        recommendationService.RecordActivity(
            new CommandActionRequest
            {
                ActionName = result.ActionName,
                ActivityType = "Command Selection"
            });

        return Ok(result);
    }
}
