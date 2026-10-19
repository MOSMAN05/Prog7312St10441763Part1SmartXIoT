
using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorsController : ControllerBase
{
    private readonly SensorRegistryService registry;
    private readonly AlertRoutingService alertService;

    public SensorsController(
        SensorRegistryService registry,
        AlertRoutingService alertService)
    {
        this.registry = registry;
        this.alertService = alertService;
    }

    [HttpGet]
    public ActionResult<List<Sensor>> GetSensors()
    {
        return Ok(registry.GetAll());
    }

    [HttpGet("{id:int}")]
    public ActionResult<Sensor> GetSensor(int id)
    {
        Sensor? sensor = registry.GetById(id);

        return sensor == null ? NotFound() : Ok(sensor);
    }

    [HttpGet("mac/{macAddress}")]
    public ActionResult<Sensor> GetByMac(string macAddress)
    {
        Sensor? sensor = registry.GetByMac(macAddress);

        return sensor == null ? NotFound() : Ok(sensor);
    }

    [HttpGet("status")]
    public ActionResult<DeviceRegistryStatus> GetStatus()
    {
        return Ok(registry.GetStatus());
    }

    [HttpPost]
    public ActionResult<Sensor> RegisterSensor(Sensor sensor)
    {
        if (!registry.TryRegister(sensor, out Sensor? registered))
        {
            return Conflict(
                "A sensor with this MAC address is already registered.");
        }

        return CreatedAtAction(
            nameof(GetSensor),
            new { id = registered!.Id },
            registered);
    }

    [HttpPut("{id:int}/connection")]
    public ActionResult<SensorConnectionResult> UpdateConnection(
        int id,
        [FromBody] SensorConnectionRequest request)
    {
        SensorConnectionResult? result = registry.SetConnection(
            id,
            request.IsOnline);

        if (result == null)
        {
            return NotFound("Sensor not found.");
        }

        // Only create an alert for a new disconnection
        if (result.NewDisconnectedAlert)
        {
            string location = result.Sensor.DeploymentLocation;
            string sourceNode = "Gateway";

            if (location.Contains(
                "Hydroponics",
                StringComparison.OrdinalIgnoreCase))
            {
                sourceNode = "Hydroponics Bay";
            }
            else if (location.Contains(
                "Utility",
                StringComparison.OrdinalIgnoreCase))
            {
                sourceNode = "Utility Room";
            }
            else if (location.Contains(
                "Zone 2",
                StringComparison.OrdinalIgnoreCase))
            {
                sourceNode = "Zone 2";
            }

            alertService.SubmitAlert(
                new AlertRequest
                {
                    SensorId = id,
                    SourceNode = sourceNode,
                    Severity = "High",
                    Message = $"{result.Sensor.DeviceName} disconnected"
                },
                dispatchCritical: false);
        }

        return Ok(result);
    }
}
