namespace HomeCycle.Application.DTOs.Responses.Auths;

public sealed class VerifyModeratorEmailResponse
{
    public string PasswordSetupToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
