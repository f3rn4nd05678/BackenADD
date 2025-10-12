namespace BackendADD.Dtos;

public class LoginRequestDto
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginResponseDto
{
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public UserInfoDto User { get; set; } = null!;
}

public class UserInfoDto
{
    public ulong Id { get; set; }
    public string Username { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public List<string> Roles { get; set; } = new();
}