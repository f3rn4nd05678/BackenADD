using BackendADD.Dtos;
using BackendADD.Infrastructure;
using BackendADD.Models;
using BackendADD.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BackendADD.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly IConfiguration _config;

    public AuthController(IUserRepository userRepo, IConfiguration config)
    {
        _userRepo = userRepo;
        _config = config;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return this.ApiBadRequest<object?>("Usuario y contraseña son requeridos", null);
        }

        var user = await _userRepo.GetByUsernameAsync(dto.Username);

        if (user == null || !user.IsActive)
        {
            return Unauthorized(ApiResponse<object?>.Fail("Credenciales inválidas", null, 401));
        }

        // Verificar contraseña (aquí debes usar tu método de hashing)
        if (!VerifyPassword(dto.Password, user.PasswordHash))
        {
            return Unauthorized(ApiResponse<object?>.Fail("Credenciales inválidas", null, 401));
        }

        // Generar JWT token
        var token = GenerateJwtToken(user);

        var response = new LoginResponseDto
        {
            Token = token,
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        };

        return this.ApiOk(response, "Login exitoso");
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserInfoDto>), 200)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim == null || !ulong.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<object?>.Fail("Token inválido", null, 401));
        }

        var user = await _userRepo.GetByIdAsync(userId);

        if (user == null || !user.IsActive)
        {
            return Unauthorized(ApiResponse<object?>.Fail("Usuario no encontrado o inactivo", null, 401));
        }

        var userInfo = new UserInfoDto
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role.ToString()
        };

        return this.ApiOk(userInfo, "Usuario actual");
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object?>), 200)]
    public IActionResult Logout()
    {
        // En implementación con JWT, el logout se maneja en el cliente
        // Aquí podrías agregar el token a una lista negra si lo deseas
        return this.ApiOk<object?>(null, "Logout exitoso");
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key no configurada");
        var jwtIssuer = _config["Jwt:Issuer"] ?? "LaSuerte";
        var jwtAudience = _config["Jwt:Audience"] ?? "LaSuerteClients";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("FullName", user.FullName)
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private bool VerifyPassword(string password, string passwordHash)
    {
        // IMPORTANTE: Implementa tu lógica de verificación de hash
        // Por ahora, comparación simple (CAMBIAR EN PRODUCCIÓN)
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}