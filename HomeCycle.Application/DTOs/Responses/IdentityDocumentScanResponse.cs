namespace HomeCycle.Application.DTOs.Responses;

public sealed class IdentityDocumentScanResponse
{
    public string? IdentityNumber { get; set; }
    public string? FullName { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public List<string> UnreadableFields { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
