using LogisticsApi.Models.DTOs;
using LogisticsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/upload")]
[Authorize]
public class UploadController(IStorageService storage) : ControllerBase
{
    private static readonly HashSet<string> _allowedContainers = ["receipts", "vehicle-photos", "documents"];
    private static readonly HashSet<string> _allowedExtensions = [".jpg", ".jpeg", ".png", ".pdf", ".webp"];

    [HttpGet("sas-token")]
    public async Task<ActionResult<SasTokenDto>> GetSasToken(
        [FromQuery] string container, [FromQuery] string fileName)
    {
        if (!_allowedContainers.Contains(container.ToLower()))
            return BadRequest(new { error = "Invalid container" });

        var ext = Path.GetExtension(fileName).ToLower();
        if (!_allowedExtensions.Contains(ext))
            return BadRequest(new { error = "File type not allowed" });

        var containerKey = container switch
        {
            "receipts" => "Receipts",
            "vehicle-photos" => "VehiclePhotos",
            "documents" => "Documents",
            _ => container
        };

        return await storage.GenerateUploadSasAsync(containerKey, fileName);
    }
}
