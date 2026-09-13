using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly TelemetryService telemetryService;

    public TelemetryController(TelemetryService telemetryService)
    {
        this.telemetryService = telemetryService;
    }

    [HttpGet("recent")]
    public ActionResult<List<TelemetryRecord>> GetRecentTelemetry()
    {
        return Ok(telemetryService.GetRecentRecords());
    }

    [HttpGet("summary")]
    public ActionResult<TelemetrySummary> GetSummary()
    {
        return Ok(telemetryService.GetSummary());
    }

    [HttpGet("history")]
    public ActionResult<float[][]> GetTemperatureHistory()
    {
        return Ok(telemetryService.GetTemperatureHistory());
    }

    [HttpPost("simulate")]
    public ActionResult<TelemetrySummary> SimulateTelemetry()
    {
        return Ok(telemetryService.GenerateBatch());
    }
}