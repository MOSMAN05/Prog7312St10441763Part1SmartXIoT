
using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly TelemetryService telemetryService;
    private readonly AlertRoutingService alertService;

    public TelemetryController(
        TelemetryService telemetryService,
        AlertRoutingService alertService)
    {
        this.telemetryService = telemetryService;
        this.alertService = alertService;
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
        TelemetrySummary summary = telemetryService.GenerateBatch();

        // Check the five new simulation readings
        SendCriticalAlerts(telemetryService.GetRecentRecords(5));

        return Ok(summary);
    }

    // Receive sensor readings through the API
    [HttpPost("ingest")]
    public ActionResult<TelemetryQueueStatus> IngestTelemetry(
        [FromBody] TelemetryEvent telemetry,
        [FromQuery] bool processImmediately = true)
    {
        TelemetryQueueStatus status = telemetryService.Enqueue(
            telemetry,
            processImmediately);

        if (processImmediately)
        {
            SendCriticalAlerts(telemetryService.GetRecentRecords(1));
        }

        return Ok(status);
    }

    // Get the current telemetry queue status
    [HttpGet("queue")]
    public ActionResult<TelemetryQueueStatus> GetQueueStatus()
    {
        return Ok(telemetryService.GetQueueStatus());
    }

    // Process buffered sensor readings
    [HttpPost("process")]
    public ActionResult<TelemetryQueueStatus> ProcessQueuedTelemetry()
    {
        int pendingCount = telemetryService.GetQueueStatus().PendingCount;

        TelemetryQueueStatus status = telemetryService.ProcessPending();

        if (pendingCount > 0)
        {
            SendCriticalAlerts(
                telemetryService.GetRecentRecords(pendingCount));
        }

        return Ok(status);
    }

    // Get indexed telemetry logs
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

    // Create critical alerts for abnormal readings
    private void SendCriticalAlerts(IEnumerable<TelemetryRecord> records)
    {
        foreach (TelemetryRecord record in records.Where(r => r.IsAnomaly))
        {
            string sourceNode = record.SensorId switch
            {
                1 => "Hydroponics Bay",
                2 => "Utility Room",
                3 => "Zone 2",
                _ => "Gateway"
            };

            alertService.SubmitAlert(new AlertRequest
            {
                SensorId = record.SensorId,
                SourceNode = sourceNode,
                Severity = "Critical",
                Message = $"{record.DeviceName}: " +
                    $"{record.Metric} reading {record.Value} {record.Unit}"
            });
        }
    }
}
