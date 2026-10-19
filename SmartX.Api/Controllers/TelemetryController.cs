
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

    // Get recent sensor readings
    [HttpGet("recent")]
    public ActionResult<List<TelemetryRecord>> GetRecentTelemetry()
    {
        return Ok(telemetryService.GetRecentRecords());
    }

    // Get the dashboard summary
    [HttpGet("summary")]
    public ActionResult<TelemetrySummary> GetSummary()
    {
        return Ok(telemetryService.GetSummary());
    }

    // Get temperature history
    [HttpGet("history")]
    public ActionResult<float[][]> GetTemperatureHistory()
    {
        return Ok(telemetryService.GetTemperatureHistory());
    }

    // Generate sample sensor readings
    [HttpPost("simulate")]
    public ActionResult<TelemetrySummary> SimulateTelemetry()
    {
        return Ok(telemetryService.GenerateBatch());
    }

    // Receive a new sensor reading
    [HttpPost("ingest")]
    public ActionResult<TelemetryQueueStatus> IngestTelemetry(
        [FromBody] TelemetryEvent telemetry,
        [FromQuery] bool processImmediately = true)
    {
        return Ok(telemetryService.Enqueue(
            telemetry,
            processImmediately));
    }

    // Check the number of readings in the queue
    [HttpGet("queue")]
    public ActionResult<TelemetryQueueStatus> GetQueueStatus()
    {
        return Ok(telemetryService.GetQueueStatus());
    }

    // Process readings waiting in the queue
    [HttpPost("process")]
    public ActionResult<TelemetryQueueStatus> ProcessQueuedTelemetry()
    {
        return Ok(telemetryService.ProcessPending());
    }

    // Get sensor logs between two dates
    [HttpGet("logs")]
    public ActionResult<List<TelemetryRecord>> GetIndexedLogs(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int limit = 200)
    {
        if (fromUtc == default || toUtc == default)
        {
            return BadRequest(
                "Provide both fromUtc and toUtc timestamps.");
        }

        if (fromUtc.ToUniversalTime() > toUtc.ToUniversalTime())
        {
            return BadRequest(
                "fromUtc must be earlier than toUtc.");
        }

        if (limit < 1 || limit > 1000)
        {
            return BadRequest(
                "Limit must be between 1 and 1000.");
        }

        return Ok(telemetryService.GetLogsByTime(
            fromUtc,
            toUtc,
            limit));
    }
}
