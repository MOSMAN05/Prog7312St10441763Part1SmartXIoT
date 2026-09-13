using Microsoft.AspNetCore.Mvc;
using SmartX.Shared;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorsController : ControllerBase
{
    private static readonly List<Sensor> sensors = new()
    {
        new Sensor
        {
            Id = 1,
            DeviceName = "Greenhouse Temperature 01",
            MacAddress = "A4:C1:38:22:0F:91",
            DeploymentLocation = "Facility A - Hydroponics Bay",
            Category = SensorCategory.Environmental,
            IsOnline = true,
            LastSeen = DateTime.UtcNow
        },
        new Sensor
        {
            Id = 2,
            DeviceName = "Smart Meter 01",
            MacAddress = "B8:27:EB:44:21:10",
            DeploymentLocation = "Facility A - Utility Room",
            Category = SensorCategory.PowerConsumption,
            IsOnline = true,
            LastSeen = DateTime.UtcNow
        },
        new Sensor
        {
            Id = 3,
            DeviceName = "Pump Controller 01",
            MacAddress = "C2:11:45:67:80:AB",
            DeploymentLocation = "Facility A - Zone 2",
            Category = SensorCategory.Actuator,
            IsOnline = false,
            LastSeen = DateTime.UtcNow.AddMinutes(-5)
        }
    };

    private static int nextId = 4;

    [HttpGet]
    public ActionResult<List<Sensor>> GetSensors()
    {
        return Ok(sensors);
    }

    [HttpGet("{id:int}")]
    public ActionResult<Sensor> GetSensor(int id)
    {
        var sensor = sensors.FirstOrDefault(s => s.Id == id);

        if (sensor == null)
        {
            return NotFound();
        }

        return Ok(sensor);
    }

    [HttpPost]
    public ActionResult<Sensor> RegisterSensor(Sensor sensor)
    {
        bool macExists = sensors.Any(s =>
            s.MacAddress.Equals(sensor.MacAddress, StringComparison.OrdinalIgnoreCase));

        if (macExists)
        {
            return Conflict("A sensor with this MAC address is already registered.");
        }

        sensor.Id = nextId++;
        sensor.IsOnline = true;
        sensor.LastSeen = DateTime.UtcNow;
        sensor.DateRegistered = DateTime.UtcNow;

        sensors.Add(sensor);

        return CreatedAtAction(
            nameof(GetSensor),
            new { id = sensor.Id },
            sensor);
    }
}