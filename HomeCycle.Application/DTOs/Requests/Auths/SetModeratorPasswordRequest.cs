namespace HomeCycle.Application.DTOs.Requests.Auths;

public sealed class SetModeratorPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
