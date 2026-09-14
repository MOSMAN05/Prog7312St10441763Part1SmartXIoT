using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;
using SmartX.Shared;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttachmentsController : ControllerBase
{
    private readonly AttachmentService attachmentService;

    public AttachmentsController(
        AttachmentService attachmentService)
    {
        this.attachmentService = attachmentService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<AttachmentRecord>> Upload(
        [FromForm] int sensorId,
        [FromForm] IFormFile file)
    {
        try
        {
            var result =
                await attachmentService.SaveFileAsync(
                    sensorId,
                    file);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("sensor/{sensorId:int}")]
    public ActionResult<List<AttachmentRecord>> GetSensorFiles(
        int sensorId)
    {
        return Ok(
            attachmentService.GetSensorFiles(sensorId));
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var record =
            attachmentService.GetFileRecord(id);

        if (record == null)
        {
            return NotFound();
        }

        var fileData =
            await attachmentService.GetFileAsync(id);

        if (fileData == null)
        {
            return NotFound();
        }

        return File(
            fileData,
            record.ContentType,
            record.FileName);
    }
}