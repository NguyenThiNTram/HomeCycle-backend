using Microsoft.AspNetCore.Http;

namespace HomeCycle.Application.DTOs.Requests;

public sealed class IdentityDocumentScanRequest
{
    public const long MaxMultipartBodyBytes = 20L * 1024 * 1024;

    public IFormFile FrontImage { get; set; } = null!;
    public IFormFile BackImage { get; set; } = null!;
    public bool ConsentConfirmed { get; set; }
}
