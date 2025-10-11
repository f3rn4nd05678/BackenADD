using BackendADD.Dtos;
using BackendADD.Infrastructure;
using BackendADD.Models;
using BackendADD.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackendADD.Controllers;

[ApiController]
[Route("api/users")]
[Authorize] // Requiere autenticación para todos los endpoints
public class UsersController : ControllerBase
{
    private readonly IAuthRepository _authRepo;
    private readonly IAuditRepository _auditRepo;

    public UsersController(IAuthRepository authRepo, IAuditRepository auditRepo)
    {
        _authRepo = authRepo;
        _auditRepo = auditRepo;
    }

    // GET: api/users
    [HttpGet]
    [Authorize(Roles = "ADMIN")] // Solo administradores
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var users = await _authRepo.GetAllActiveUsersAsync();

        var userDtos = users.Select(u => new UserDto
        {
            UserId = u.UserId,
            Username = u.Username,
            FullName = u.FullName,
            Role = u.Role.ToString(),
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        });

        return this.ApiOk(userDtos, "Lista de usuarios");
    }

    // GET: api/users/{id}
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 404)]
    public async Task<IActionResult> GetById(ulong id)
    {
        // Verificar permisos: ADMIN puede ver todos, EMPLOYEE solo puede verse a sí mismo
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();

        if (currentUserRole != "ADMIN" && currentUserId != id)
        {
            return this.ApiForbidden("No tienes permiso para ver este usuario");
        }

        var user = await _authRepo.GetUserByIdAsync(id);

        if (user == null)
        {
            return this.ApiNotFound("Usuario no encontrado");
        }

        var userDto = new UserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };

        return this.ApiOk(userDto, "Usuario encontrado");
    }

    // POST: api/users
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 400)]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(dto.Username))
            return this.ApiBadRequest("El nombre de usuario es requerido", new { field = "username" });

        if (string.IsNullOrWhiteSpace(dto.Password))
            return this.ApiBadRequest("La contraseña es requerida", new { field = "password" });

        if (dto.Password.Length < 6)
            return this.ApiBadRequest("La contraseña debe tener al menos 6 caracteres", new { field = "password" });

        if (string.IsNullOrWhiteSpace(dto.FullName))
            return this.ApiBadRequest("El nombre completo es requerido", new { field = "fullName" });

        // Verificar si el username ya existe
        if (await _authRepo.UsernameExistsAsync(dto.Username))
        {
            return this.ApiBadRequest("El nombre de usuario ya existe", new { field = "username" });
        }

        // Parsear rol
        if (!Enum.TryParse<UserRole>(dto.Role, true, out var role))
        {
            return this.ApiBadRequest("Rol inválido. Usa 'ADMIN' o 'EMPLOYEE'", new { field = "role" });
        }

        // Crear usuario
        var user = await _authRepo.CreateUserAsync(dto.Username, dto.Password, dto.FullName, role);

        if (user == null)
        {
            return this.ApiBadRequest("No se pudo crear el usuario", null);
        }

        // Auditoría
        await _auditRepo.LogActionAsync(
            currentUserId,
            "DEACTIVATE_USER",
            "users",
            id,
            null
        );

        return this.ApiOk<object?>(null, "Usuario desactivado exitosamente");
    }

    // POST: api/users/change-password
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse<object?>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var currentUserId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            return this.ApiBadRequest("Se requieren ambas contraseñas", null);
        }

        if (dto.NewPassword.Length < 6)
        {
            return this.ApiBadRequest("La nueva contraseña debe tener al menos 6 caracteres", new { field = "newPassword" });
        }

        if (dto.NewPassword == dto.CurrentPassword)
        {
            return this.ApiBadRequest("La nueva contraseña debe ser diferente a la actual", new { field = "newPassword" });
        }

        var success = await _authRepo.ChangePasswordAsync(currentUserId, dto.CurrentPassword, dto.NewPassword);

        if (!success)
        {
            return this.ApiBadRequest("Contraseña actual incorrecta", new { field = "currentPassword" });
        }

        // Auditoría
        await _auditRepo.LogActionAsync(
            currentUserId,
            "CHANGE_PASSWORD",
            "users",
            currentUserId,
            null
        );

        return this.ApiOk<object?>(null, "Contraseña actualizada exitosamente");
    }

    // POST: api/users/{id}/reset-password (Solo ADMIN)
    [HttpPost("{id}/reset-password")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 404)]
    public async Task<IActionResult> ResetPassword(ulong id, [FromBody] ResetPasswordDto dto)
    {
        var user = await _authRepo.GetUserByIdAsync(id);

        if (user == null)
        {
            return this.ApiNotFound("Usuario no encontrado");
        }

        string newPassword = dto.NewPassword;

        // Si no se proporciona contraseña, generar una aleatoria
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            newPassword = GenerateRandomPassword(10);
        }
        else if (newPassword.Length < 6)
        {
            return this.ApiBadRequest("La contraseña debe tener al menos 6 caracteres", new { field = "newPassword" });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 11);
        var success = await _authRepo.UpdateUserAsync(user);

        if (!success)
        {
            return this.ApiBadRequest("No se pudo resetear la contraseña", null);
        }

        // Auditoría
        var currentUserId = GetCurrentUserId();
        await _auditRepo.LogActionAsync(
            currentUserId,
            "RESET_PASSWORD",
            "users",
            id,
            new { resetBy = currentUserId }
        );

        var response = new ResetPasswordResponseDto
        {
            UserId = user.UserId,
            Username = user.Username,
            TemporaryPassword = newPassword
        };

        return this.ApiOk(response, "Contraseña reseteada exitosamente");
    }

    // GET: api/users/{id}/audit-logs
    [HttpGet("{id}/audit-logs")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AuditLog>>), 200)]
    public async Task<IActionResult> GetUserAuditLogs(ulong id, [FromQuery] int limit = 50)
    {
        var logs = await _auditRepo.GetLogsByUserAsync(id, limit);
        return this.ApiOk(logs, $"Logs de auditoría del usuario {id}");
    }

    // ============ MÉTODOS PRIVADOS ============

    private ulong GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return ulong.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }

    private string GenerateRandomPassword(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
var currentUserId = GetCurrentUserId();
await _auditRepo.LogActionAsync(
    currentUserId,
            "CREATE_USER",
            "users",
    user.UserId,
            new { username = user.Username, role = user.Role.ToString() }
        );

var userDto = new UserDto
{
    UserId = user.UserId,
    Username = user.Username,
    FullName = user.FullName,
    Role = user.Role.ToString(),
    IsActive = user.IsActive,
    CreatedAt = user.CreatedAt
};

return this.ApiCreated(userDto, "Usuario creado exitosamente");
    }

    // PUT: api/users/{id}
    [HttpPut("{id}")]
[Authorize(Roles = "ADMIN")]
[ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
[ProducesResponseType(typeof(ApiResponse<object?>), 404)]
public async Task<IActionResult> Update(ulong id, [FromBody] UpdateUserDto dto)
{
    var user = await _authRepo.GetUserByIdAsync(id);

    if (user == null)
    {
        return this.ApiNotFound("Usuario no encontrado");
    }

    // Actualizar campos si se proporcionan
    if (!string.IsNullOrWhiteSpace(dto.FullName))
    {
        user.FullName = dto.FullName.Trim();
    }

    if (!string.IsNullOrWhiteSpace(dto.Role))
    {
        if (Enum.TryParse<UserRole>(dto.Role, true, out var role))
        {
            user.Role = role;
        }
        else
        {
            return this.ApiBadRequest("Rol inválido", new { field = "role" });
        }
    }

    // Si se proporciona nueva contraseña
    if (!string.IsNullOrWhiteSpace(dto.Password))
    {
        if (dto.Password.Length < 6)
        {
            return this.ApiBadRequest("La contraseña debe tener al menos 6 caracteres", new { field = "password" });
        }
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, 11);
    }

    var success = await _authRepo.UpdateUserAsync(user);

    if (!success)
    {
        return this.ApiBadRequest("No se pudo actualizar el usuario", null);
    }

    // Auditoría
    var currentUserId = GetCurrentUserId();
    await _auditRepo.LogActionAsync(
        currentUserId,
        "UPDATE_USER",
        "users",
        id,
        new { fullName = dto.FullName, role = dto.Role }
    );

    var userDto = new UserDto
    {
        UserId = user.UserId,
        Username = user.Username,
        FullName = user.FullName,
        Role = user.Role.ToString(),
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };

    return this.ApiOk(userDto, "Usuario actualizado exitosamente");
}

// DELETE: api/users/{id} (Desactivar)
[HttpDelete("{id}")]
[Authorize(Roles = "ADMIN")]
[ProducesResponseType(typeof(ApiResponse<object?>), 200)]
[ProducesResponseType(typeof(ApiResponse<object?>), 404)]
public async Task<IActionResult> Deactivate(ulong id)
{
    var currentUserId = GetCurrentUserId();

    // No permitir que el admin se desactive a sí mismo
    if (currentUserId == id)
    {
        return this.ApiBadRequest("No puedes desactivarte a ti mismo", null);
    }

    var success = await _authRepo.DeactivateUserAsync(id);

    if (!success)
    {
        return this.ApiNotFound("Usuario no encontrado");
    }

        // Auditoría