using System.ComponentModel.DataAnnotations;

namespace HomeCycle.Application.DTOs.Requests.Auths;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email format is invalid.")]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest : ForgotPasswordRequest
{
    [Required(ErrorMessage = "OTP is required.")]
    [RegularExpression(@"\A[0-9]{6}\z", ErrorMessage = "OTP must contain exactly 6 digits.")]
    public string Otp { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [MaxLength(50, ErrorMessage = "Password must not exceed 50 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Confirm password must match the new password.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
