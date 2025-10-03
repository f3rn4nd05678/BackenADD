using BackendADD.Data;
using BackendADD.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendADD.Repositories;

public class LotteryTypeRepository : ILotteryTypeRepository
{
    private readonly AppDbContext _db;
    public LotteryTypeRepository(AppDbContext db) => _db = db;

    public async Task<List<LotteryType>> GetAllAsync(bool? onlyActive = null)
    {
        var q = _db.LotteryTypes.AsQueryable();
        if (onlyActive.HasValue) q = q.Where(t => t.IsActive == onlyActive.Value);
        return await q.OrderBy(t => t.Name).ToListAsync();
    }

    public Task<LotteryType?> GetByIdAsync(ulong id) => _db.LotteryTypes.FindAsync(id).AsTask();

    public async Task<LotteryType> AddAsync(LotteryType entity)
    {
        await _db.LotteryTypes.AddAsync(entity);
        return entity;
    }

    public Task UpdateAsync(LotteryType entity)
    {
        _db.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task SaveAsync() => _db.SaveChangesAsync();
}
