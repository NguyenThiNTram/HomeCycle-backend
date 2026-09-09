namespace HomeCycle.Application.DTOs.Requests.Auths;

public sealed class CreateModeratorRequest
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
