using BackendADD.Data;
using BackendADD.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendADD.Repositories;

public interface IAppSettingsRepository
{
    Task<string?> GetAsync(string key);
    Task<decimal?> GetDecimalAsync(string key);
    Task<int?> GetIntAsync(string key);
    Task SetAsync(string key, string value);
    Task<Dictionary<string, string>> GetAllAsync();
}

public class AppSettingsRepository : IAppSettingsRepository
{
    private readonly AppDbContext _db;

    public AppSettingsRepository(AppDbContext db) => _db = db;

    public async Task<string?> GetAsync(string key)
    {
        var setting = await _db.AppSettings.FindAsync(key);
        return setting?.V;
    }

    public async Task<decimal?> GetDecimalAsync(string key)
    {
        var value = await GetAsync(key);
        return value != null && decimal.TryParse(value, out var result) ? result : null;
    }

    public async Task<int?> GetIntAsync(string key)
    {
        var value = await GetAsync(key);
        return value != null && int.TryParse(value, out var result) ? result : null;
    }

    public async Task SetAsync(string key, string value)
    {
        var setting = await _db.AppSettings.FindAsync(key);
        if (setting == null)
        {
            setting = new AppSetting { K = key, V = value };
            await _db.AppSettings.AddAsync(setting);
        }
        else
        {
            setting.V = value;
            _db.Entry(setting).State = EntityState.Modified;
        }
        await _db.SaveChangesAsync();
    }

    public async Task<Dictionary<string, string>> GetAllAsync()
    {
        return await _db.AppSettings.ToDictionaryAsync(s => s.K, s => s.V);
    }
}