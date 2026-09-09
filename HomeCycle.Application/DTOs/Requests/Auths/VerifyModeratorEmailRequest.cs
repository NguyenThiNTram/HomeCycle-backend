namespace HomeCycle.Application.DTOs.Requests.Auths;

public sealed class VerifyModeratorEmailRequest
{
    public string Token { get; set; } = string.Empty;
}
