using BackendADD.Data;
using BackendADD.Dtos;
using BackendADD.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendADD.Repositories;

public interface IBetRepository
{
    Task<List<Bet>> GetAllAsync(ulong? eventId = null, ulong? customerId = null, BetState? state = null);
    Task<Bet?> GetByIdAsync(ulong id);
    Task<BetDetailDto?> GetByIdWithDetailsAsync(ulong id);
    Task<Bet?> GetByQrTokenAsync(string qrToken);
    Task<BetResultDto?> GetBetResultByQrAsync(string qrToken);
    Task<BetVoucherDto?> GetVoucherDataAsync(ulong betId);
    Task<EventBetSummaryDto?> GetEventSummaryAsync(ulong eventId);
    Task<List<DailyWinnerDto>> GetDailyWinnersAsync(DateOnly date);
    Task<Bet> AddAsync(Bet entity);
    Task UpdateAsync(Bet entity);
    Task SaveAsync();
    Task<Customer?> GetCustomerAsync(ulong customerId);
}

public class BetRepository : IBetRepository
{
    private readonly AppDbContext _db;

    public BetRepository(AppDbContext db) => _db = db;

    public async Task<List<Bet>> GetAllAsync(
        ulong? eventId = null,
        ulong? customerId = null,
        BetState? state = null)
    {
        var query = _db.Bets.AsQueryable();

        if (eventId.HasValue)
            query = query.Where(b => b.EventId == eventId.Value);

        if (customerId.HasValue)
            query = query.Where(b => b.CustomerId == customerId.Value);

        if (state.HasValue)
            query = query.Where(b => b.State == state.Value);

        return await query
            .OrderByDescending(b => b.PlacedAt)
            .ToListAsync();
    }

    public Task<Bet?> GetByIdAsync(ulong id)
        => _db.Bets.FindAsync(id).AsTask();

    public async Task<BetDetailDto?> GetByIdWithDetailsAsync(ulong id)
    {
        var result = await (
            from b in _db.Bets
            join e in _db.LotteryEvents on b.EventId equals e.Id
            join lt in _db.LotteryTypes on e.LotteryTypeId equals lt.Id
            join c in _db.Customers on b.CustomerId equals c.Id
            join u in _db.Users on b.UserId equals u.Id
            where b.Id == id
            select new BetDetailDto(
                b.Id,
                e.Id,
                lt.Name,
                e.EventDate,
                e.EventNumberOfDay,
                c.FullName,
                b.NumberPlayed,
                b.Amount,
                b.PlacedAt,
                b.QrToken,
                b.State,
                u.FullName
            )
        ).FirstOrDefaultAsync();

        return result;
    }

    public Task<Bet?> GetByQrTokenAsync(string qrToken)
        => _db.Bets.FirstOrDefaultAsync(b => b.QrToken == qrToken);

    public async Task<BetResultDto?> GetBetResultByQrAsync(string qrToken)
    {
        var query = from b in _db.Bets
                    join e in _db.LotteryEvents on b.EventId equals e.Id
                    join lt in _db.LotteryTypes on e.LotteryTypeId equals lt.Id
                    join c in _db.Customers on b.CustomerId equals c.Id
                    join p in _db.Payouts on b.Id equals p.BetId into payouts
                    from p in payouts.DefaultIfEmpty()
                    where b.QrToken == qrToken
                    select new
                    {
                        Bet = b,
                        Event = e,
                        LotteryType = lt,
                        Customer = c,
                        Payout = p
                    };

        var data = await query.FirstOrDefaultAsync();
        if (data == null) return null;

        var isWinner = data.Event.WinningNumber.HasValue &&
                      data.Event.WinningNumber.Value == data.Bet.NumberPlayed;

        return new BetResultDto(
            data.Bet.Id,
            data.Customer.FullName,
            data.LotteryType.Name,
            data.Event.EventDate,
            data.Event.EventNumberOfDay,
            data.Bet.NumberPlayed,
            data.Bet.Amount,
            data.Bet.PlacedAt,
            data.Event.State,
            data.Event.WinningNumber,
            isWinner,
            data.Payout?.CalculatedPrize,
            data.Payout?.BirthdayBonusApplied,
            data.Payout?.PaidAt,
            data.Payout?.ReceiptNumber,
            data.Bet.State
        );
    }

    public async Task<BetVoucherDto?> GetVoucherDataAsync(ulong betId)
    {
        var result = await (
            from b in _db.Bets
            join e in _db.LotteryEvents on b.EventId equals e.Id
            join lt in _db.LotteryTypes on e.LotteryTypeId equals lt.Id
            join c in _db.Customers on b.CustomerId equals c.Id
            join u in _db.Users on b.UserId equals u.Id
            where b.Id == betId
            select new BetVoucherDto(
                b.Id,
                b.QrToken,
                c.FullName,
                c.Phone,
                lt.Name,
                e.EventDate,
                e.EventNumberOfDay,
                b.NumberPlayed,
                b.Amount,
                b.PlacedAt,
                u.FullName
            )
        ).FirstOrDefaultAsync();

        return result;
    }

    public async Task<EventBetSummaryDto?> GetEventSummaryAsync(ulong eventId)
    {
        var evt = await (
            from e in _db.LotteryEvents
            join lt in _db.LotteryTypes on e.LotteryTypeId equals lt.Id
            where e.Id == eventId
            select new { Event = e, LotteryType = lt }
        ).FirstOrDefaultAsync();

        if (evt == null) return null;

        var bets = await _db.Bets
            .Where(b => b.EventId == eventId)
            .ToListAsync();

        var betsByNumber = bets
            .GroupBy(b => b.NumberPlayed)
            .ToDictionary(g => g.Key, g => g.Count());

        return new EventBetSummaryDto(
            evt.Event.Id,
            evt.LotteryType.Name,
            evt.Event.EventDate,
            evt.Event.EventNumberOfDay,
            bets.Sum(b => b.Amount),
            bets.Count,
            betsByNumber,
            evt.Event.State
        );
    }

    public async Task<List<DailyWinnerDto>> GetDailyWinnersAsync(DateOnly date)
    {
        var events = await (
            from e in _db.LotteryEvents
            join lt in _db.LotteryTypes on e.LotteryTypeId equals lt.Id
            where e.EventDate == date && e.State == EventState.RESULTS_PUBLISHED
            orderby lt.Name, e.EventNumberOfDay
            select new { Event = e, LotteryType = lt }
        ).ToListAsync();

        var winners = new List<DailyWinnerDto>();

        foreach (var evt in events)
        {
            if (!evt.Event.WinningNumber.HasValue)
            {
                winners.Add(new DailyWinnerDto(
                    evt.LotteryType.Name,
                    evt.Event.EventNumberOfDay,
                    null,
                    "DESIERTO",
                    null,
                    null,
                    evt.Event.ResultsPublishedAt
                ));
                continue;
            }

            var winningBets = await (
                from b in _db.Bets
                join c in _db.Customers on b.CustomerId equals c.Id
                join p in _db.Payouts on b.Id equals p.BetId into payouts
                from p in payouts.DefaultIfEmpty()
                where b.EventId == evt.Event.Id &&
                      b.NumberPlayed == evt.Event.WinningNumber.Value
                select new { Bet = b, Customer = c, Payout = p }
            ).ToListAsync();

            if (!winningBets.Any())
            {
                winners.Add(new DailyWinnerDto(
                    evt.LotteryType.Name,
                    evt.Event.EventNumberOfDay,
                    evt.Event.WinningNumber.Value,
                    "DESIERTO",
                    null,
                    null,
                    evt.Event.ResultsPublishedAt
                ));
            }
            else
            {
                foreach (var win in winningBets)
                {
                    winners.Add(new DailyWinnerDto(
                        evt.LotteryType.Name,
                        evt.Event.EventNumberOfDay,
                        evt.Event.WinningNumber.Value,
                        "WINNER",
                        win.Customer.FullName,
                        win.Payout?.CalculatedPrize,
                        evt.Event.ResultsPublishedAt
                    ));
                }
            }
        }

        return winners;
    }

    public async Task<Bet> AddAsync(Bet entity)
    {
        await _db.Bets.AddAsync(entity);
        return entity;
    }

    public Task UpdateAsync(Bet entity)
    {
        _db.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task SaveAsync() => _db.SaveChangesAsync();

    public async Task<Customer?> GetCustomerAsync(ulong customerId)
    {
        return await _db.Customers.FindAsync(customerId);
    }
}