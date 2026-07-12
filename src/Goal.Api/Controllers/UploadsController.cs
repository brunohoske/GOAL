using Goal.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Goal.Api.Controllers;

[Authorize]
public class UploadsController : ApiControllerBase
{
    private readonly IFileStorage _storage;

    public UploadsController(IFileStorage storage) => _storage = storage;

    /// <summary>Uploads a file (completion image/attachment) and returns its public URL.</summary>
    [HttpPost]
    [RequestSizeLimit(15_000_000)] // 15 MB
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) return BadRequest("Empty file.");
        await using var stream = file.OpenReadStream();
        var url = await _storage.SaveAsync(stream, file.FileName, file.ContentType, ct);
        return Ok(new { url });
    }
}
