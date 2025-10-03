using BackendADD.Data;
using BackendADD.Dtos;
using BackendADD.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendADD.Repositories;

public interface IPayoutRepository
{
    Task<Payout?> GetByIdAsync(ulong id);
    Task<Payout?> GetByBetIdAsync(ulong betId);
    Task<List<PendingPayoutDto>> GetPendingPayoutsAsync();
    Task<Payout> AddAsync(Payout entity);
    Task UpdateAsync(Payout entity);
    Task SaveAsync();
}

public class PayoutRepository : IPayoutRepository
{
    private readonly AppDbContext _db;
    private readonly IAppSettingsRepository _settingsRepo;

    public PayoutRepository(AppDbContext db, IAppSettingsRepository settingsRepo)
    {
        _db = db;
        _settingsRepo = settingsRepo;
    }

    public Task<Payout?> GetByIdAsync(ulong id)
        => _db.Payouts.FindAsync(id).AsTask();

    public Task<Payout?> GetByBetIdAsync(ulong betId)
        => _db.Payouts.FirstOrDefaultAsync(p => p.BetId == betId);

    public async Task<List<PendingPayoutDto>> GetPendingPayoutsAsync()
    {
        var claimDays = await _settingsRepo.GetIntAsync("PRIZE_CLAIM_BUSINESS_DAYS") ?? 5;

        var query = from b in _db.Bets
                    join e in _db.LotteryEvents on b.EventId equals e.Id
                    join lt in _db.LotteryTypes on e.LotteryTypeId equals lt.Id
                    join c in _db.Customers on b.CustomerId equals c.Id
                    where e.State == EventState.RESULTS_PUBLISHED &&
                          e.WinningNumber.HasValue &&
                          b.NumberPlayed == e.WinningNumber.Value &&
                          b.State == BetState.WIN_PENDING &&
                          !_db.Payouts.Any(p => p.BetId == b.Id)
                    select new
                    {
                        Bet = b,
                        Event = e,
                        LotteryType = lt,
                        Customer = c
                    };

        var data = await query.ToListAsync();

        var results = new List<PendingPayoutDto>();

        foreach (var item in data)
        {
            var basePrize = item.Bet.Amount * item.LotteryType.PayoutFactor;
            var isBirthday = item.Customer.BirthDate.HasValue &&
                           item.Customer.BirthDate.Value.Month == item.Event.EventDate.Month &&
                           item.Customer.BirthDate.Value.Day == item.Event.EventDate.Day;

            var bonusPercent = await _settingsRepo.GetDecimalAsync("BIRTHDAY_BONUS_PERCENT") ?? 10m;
            var birthdayBonus = isBirthday ? basePrize * (bonusPercent / 100) : 0m;
            var totalPrize = basePrize + birthdayBonus;

            var expirationDate = item.Event.ResultsPublishedAt!.Value.AddDays(claimDays);
            var daysRemaining = (expirationDate - DateTime.UtcNow).Days;

            results.Add(new PendingPayoutDto(
                item.Bet.Id,
                item.Customer.FullName,
                item.LotteryType.Name,
                item.Event.EventDate,
                item.Event.EventNumberOfDay,
                item.Bet.NumberPlayed,
                item.Bet.Amount,
                totalPrize,
                isBirthday,
                item.Event.ResultsPublishedAt.Value,
                expirationDate,
                Math.Max(0, daysRemaining)
            ));
        }

        return results.OrderBy(p => p.DaysRemaining).ToList();
    }

    public async Task<Payout> AddAsync(Payout entity)
    {
        await _db.Payouts.AddAsync(entity);
        return entity;
    }

    public Task UpdateAsync(Payout entity)
    {
        _db.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task SaveAsync() => _db.SaveChangesAsync();
}