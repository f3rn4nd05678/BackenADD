using BackendADD.Dtos;
using BackendADD.Infrastructure;
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
    private readonly IAuthRepository _authRepo;
    private readonly IConfiguration _config;

    public AuthController(IAuthRepository authRepo, IConfiguration config)
    {
        _authRepo = authRepo;
        _config = config;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return this.ApiBadRequest<object>("Usuario y contraseña son requeridos");

        var user = await _authRepo.ValidateCredentialsAsync(request.Username, request.Password);
        if (user == null)
            return this.ApiUnauthorized("Credenciales inválidas");

        var roles = await _authRepo.GetUserRolesAsync(user.Id);
        var token = GenerateJwtToken(user, roles);
        var expiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours());

        var response = new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserInfoDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Roles = roles
            }
        };

        return this.ApiOk(response, "Login exitoso");
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !ulong.TryParse(userIdClaim, out var userId))
            return this.ApiUnauthorized("Token inválido");

        var userInfo = await _authRepo.GetUserInfoAsync(userId);
        if (userInfo == null)
            return this.ApiUnauthorized("Usuario no encontrado");

        return this.ApiOk(userInfo, "Usuario autenticado");
    }

    private string GenerateJwtToken(BackendADD.Models.User user, List<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtKey()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("full_name", user.FullName)
        };

        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: GetJwtIssuer(),
            audience: GetJwtAudience(),
            claims: claims,
            expires: DateTime.UtcNow.AddHours(GetTokenExpirationHours()),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GetJwtKey() => _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key no configurada");
    private string GetJwtIssuer() => _config["Jwt:Issuer"] ?? "LaSuerteAPI";
    private string GetJwtAudience() => _config["Jwt:Audience"] ?? "LaSuerteClient";
    private int GetTokenExpirationHours() => int.TryParse(_config["Jwt:ExpirationHours"], out var hours) ? hours : 8;
}