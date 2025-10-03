using BackendADD.Data;
using BackendADD.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendADD.Repositories;

public interface ILotteryEventRepository
{
    Task<List<LotteryEvent>> GetAllAsync(DateOnly? date = null, ulong? lotteryTypeId = null, EventState? state = null);
    Task<LotteryEvent?> GetByIdAsync(ulong id);
    Task<List<LotteryEvent>> GetOpenEventsAsync();
    Task<LotteryEvent?> GetEventForBettingAsync(ulong lotteryTypeId, DateOnly date, int eventNumber);
    Task<LotteryEvent> AddAsync(LotteryEvent entity);
    Task UpdateAsync(LotteryEvent entity);
    Task SaveAsync();
}

public class LotteryEventRepository : ILotteryEventRepository
{
    private readonly AppDbContext _db;

    public LotteryEventRepository(AppDbContext db) => _db = db;

    public async Task<List<LotteryEvent>> GetAllAsync(
        DateOnly? date = null,
        ulong? lotteryTypeId = null,
        EventState? state = null)
    {
        var query = _db.LotteryEvents.AsQueryable();

        if (date.HasValue)
            query = query.Where(e => e.EventDate == date.Value);

        if (lotteryTypeId.HasValue)
            query = query.Where(e => e.LotteryTypeId == lotteryTypeId.Value);

        if (state.HasValue)
            query = query.Where(e => e.State == state.Value);

        return await query
            .OrderBy(e => e.EventDate)
            .ThenBy(e => e.LotteryTypeId)
            .ThenBy(e => e.EventNumberOfDay)
            .ToListAsync();
    }

    public Task<LotteryEvent?> GetByIdAsync(ulong id)
        => _db.LotteryEvents.FindAsync(id).AsTask();

    public async Task<List<LotteryEvent>> GetOpenEventsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await _db.LotteryEvents
            .Where(e => e.EventDate == today && e.State == EventState.OPEN)
            .OrderBy(e => e.LotteryTypeId)
            .ThenBy(e => e.EventNumberOfDay)
            .ToListAsync();
    }

    public async Task<LotteryEvent?> GetEventForBettingAsync(
        ulong lotteryTypeId,
        DateOnly date,
        int eventNumber)
    {
        return await _db.LotteryEvents
            .FirstOrDefaultAsync(e =>
                e.LotteryTypeId == lotteryTypeId &&
                e.EventDate == date &&
                e.EventNumberOfDay == eventNumber &&
                e.State == EventState.OPEN
            );
    }

    public async Task<LotteryEvent> AddAsync(LotteryEvent entity)
    {
        await _db.LotteryEvents.AddAsync(entity);
        return entity;
    }

    public Task UpdateAsync(LotteryEvent entity)
    {
        _db.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task SaveAsync() => _db.SaveChangesAsync();
}