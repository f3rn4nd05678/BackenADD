using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackendADD.Models;

public enum UserRole : byte
{
    ADMIN = 1,
    EMPLOYEE = 2
}

[Table("users")]
public class User
{
    [Key]
    [Column("user_id")]
    public ulong UserId { get; set; }

    [Column("username")]
    [MaxLength(50)]
    public string Username { get; set; } = null!;

    [Column("password_hash")]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = null!;

    [Column("full_name")]
    [MaxLength(100)]
    public string FullName { get; set; } = null!;

    [Column("role")]
    public UserRole Role { get; set; } = UserRole.EMPLOYEE;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// DTOs adicionales para gestión de usuarios
public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    public string? NewPassword { get; set; }
}

public class ResetPasswordResponseDto
{
    public ulong UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
}