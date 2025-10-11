using BackendADD.Data;
using BackendADD.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendADD.Repositories;

public interface IAuthRepository
{
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserByIdAsync(ulong userId);
    Task<bool> ValidateCredentialsAsync(string username, string password);
    Task<User?> CreateUserAsync(string username, string password, string fullName, UserRole role);
    Task<bool> ChangePasswordAsync(ulong userId, string currentPassword, string newPassword);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeactivateUserAsync(ulong userId);
    Task<bool> UsernameExistsAsync(string username);
    Task<IEnumerable<User>> GetAllActiveUsersAsync();
}

public class AuthRepository : IAuthRepository
{
    private readonly AppDbContext _db;

    public AuthRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetUserByIdAsync(ulong userId)
    {
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        var user = await GetUserByUsernameAsync(username);

        if (user == null || !user.IsActive)
            return false;

        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    public async Task<User?> CreateUserAsync(string username, string password, string fullName, UserRole role)
    {
        // Verificar si el usuario ya existe
        if (await UsernameExistsAsync(username))
            return null;

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, 11);

        var user = new User
        {
            Username = username.Trim().ToLower(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        return user;
    }

    public async Task<bool> ChangePasswordAsync(ulong userId, string currentPassword, string newPassword)
    {
        var user = await _db.Users.FindAsync(userId);

        if (user == null || !user.IsActive)
            return false;

        // Verificar contraseña actual
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return false;

        // Actualizar con nueva contraseña
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 11);

        _db.Entry(user).State = EntityState.Modified;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        var existingUser = await _db.Users.FindAsync(user.UserId);

        if (existingUser == null)
            return false;

        existingUser.FullName = user.FullName;
        existingUser.Role = user.Role;
        existingUser.IsActive = user.IsActive;

        _db.Entry(existingUser).State = EntityState.Modified;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeactivateUserAsync(ulong userId)
    {
        var user = await _db.Users.FindAsync(userId);

        if (user == null)
            return false;

        user.IsActive = false;

        _db.Entry(user).State = EntityState.Modified;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _db.Users
            .AnyAsync(u => u.Username == username.Trim().ToLower());
    }

    public async Task<IEnumerable<User>> GetAllActiveUsersAsync()
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }
}