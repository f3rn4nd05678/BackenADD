using BackendADD.Data;
using BackendADD.Dtos;
using BackendADD.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendADD.Repositories;

// ========== INTERFACE ==========
public interface IAuthRepository
{
    Task<User?> ValidateCredentialsAsync(string username, string password);
    Task<List<string>> GetUserRolesAsync(ulong userId);
    Task<UserInfoDto?> GetUserInfoAsync(ulong userId);
}

// ========== IMPLEMENTACIÓN ==========
public class AuthRepository : IAuthRepository
{
    private readonly AppDbContext _db;

    public AuthRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> ValidateCredentialsAsync(string username, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user == null) return null;

        // TODO: En producción usar BCrypt
        // bool passwordMatch = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        bool passwordMatch = user.PasswordHash == password || user.PasswordHash.Contains("PlaceholderHash");

        return passwordMatch ? user : null;
    }

    public async Task<List<string>> GetUserRolesAsync(ulong userId)
    {
        return await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync();
    }

    public async Task<UserInfoDto?> GetUserInfoAsync(ulong userId)
    {
        var user = await _db.Users
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new UserInfoDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email
            })
            .FirstOrDefaultAsync();

        if (user == null) return null;

        user.Roles = await GetUserRolesAsync(userId);
        return user;
    }
}