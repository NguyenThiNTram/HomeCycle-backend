using HomeCycle.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeCycle.Domain.Entities;

public class user
{
    public Guid UserId { get; set; }

    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;

    public bool IsEmailVerified { get; set; }

    public string Password { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }

    public string? AvatarUrl { get; set; }

    public UserRole Role { get; set; }

    public UserStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public user()
    {
    }

    public user(Guid UserId, string Username, string Email)
    {
        this.UserId = UserId;
        this.Username = Username;
        this.Email = Email;
    }
}
