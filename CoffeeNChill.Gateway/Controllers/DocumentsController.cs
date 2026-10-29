using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize(Roles = "Admin,Staff")]
public sealed class DocumentsController(BackendApiService backend) : ControllerBase
{
    /// <summary>Uploads a staff document using multipart/form-data with a file field.</summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(CancellationToken ct)
    {
        await backend.ForwardIncomingAsync(HttpContext, "api/documents/upload", ct);
        return new EmptyResult();
    }

    /// <summary>Lists stored staff documents.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        await backend.ForwardAsync(HttpContext, "api/documents", ct);
        return new EmptyResult();
    }

    /// <summary>Downloads a stored staff document by filename.</summary>
    [HttpGet("download/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download([FromRoute] string fileName, CancellationToken ct)
    {
        await backend.ForwardAsync(HttpContext, $"api/documents/download/{Uri.EscapeDataString(fileName)}", ct);
        return new EmptyResult();
    }
}
