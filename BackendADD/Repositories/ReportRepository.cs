using BackendADD.Data;
using BackendADD.Dtos;
using BackendADD.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendADD.Repositories;

public interface IReportRepository
{
    Task<CollectionReportDto> GetCollectionReportAsync(
        DateOnly startDate, DateOnly endDate,
        ulong? lotteryTypeId = null, int? eventNumberOfDay = null);
    Task<List<DailyCollectionDto>> GetDailyCollectionAsync(int year, int month);
    Task<List<TopWinnerDto>> GetTopWinnersByFrequencyAsync(int year, int month, int top = 10);
    Task<List<TopWinnerDto>> GetTopWinnersByAmountAsync(int year, int month, int top = 10);
    Task<DailySummaryDto> GetDailySummaryAsync(DateOnly date);
    Task<LotteryTypeDetailDto> GetLotteryTypeDetailAsync(
        ulong lotteryTypeId, DateOnly startDate, DateOnly endDate);
}

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _db;

    public ReportRepository(AppDbContext db) => _db = db;

    public async Task<CollectionReportDto> GetCollectionReportAsync(
        DateOnly startDate, DateOnly endDate,
        ulong? lotteryTypeId = null, int? eventNumberOfDay = null)
    {
        var query = from e in _db.LotteryEvents
                    join lt in _db.LotteryTypes on e.LotteryTypeId equals lt.Id
                    where e.EventDate >= startDate && e.EventDate <= endDate
                    select new { Event = e, LotteryType = lt };

        if (lotteryTypeId.HasValue)
            query = query.Where(x => x.Event.LotteryTypeId == lotteryTypeId.Value);

        if (eventNumberOfDay.HasValue)
            query = query.Where(x => x.Event.EventNumberOfDay == eventNumberOfDay.Value);

        var events = await query.ToListAsync();
        var eventIds = events.Select(x => x.Event.Id).ToList();

        var bets = await _db.Bets
            .Where(b => eventIds.Contains(b.EventId))
            .ToListAsync();

        var eventDetails = events.Select(evt =>
        {
            var eventBets = bets.Where(b => b.EventId == evt.Event.Id).ToList();
            return new EventCollectionDto(
                evt.Event.Id,
                evt.LotteryType.Name,
                evt.Event.EventDate,
                evt.Event.EventNumberOfDay,
                eventBets.Sum(b => b.Amount),
                eventBets.Count,
                evt.Event.State.ToString()
            );
        }).OrderBy(e => e.EventDate)
          .ThenBy(e => e.LotteryTypeName)
          .ThenBy(e => e.EventNumberOfDay)
          .ToList();

        var totalCollected = bets.Sum(b => b.Amount);
        var totalBets = bets.Count;
        var avgBet = totalBets > 0 ? totalCollected / totalBets : 0;

        string? lotteryTypeName = lotteryTypeId.HasValue
            ? events.FirstOrDefault()?.LotteryType.Name
            : null;

        return new CollectionReportDto(
            startDate,
            endDate,
            lotteryTypeName,
            eventNumberOfDay,
            totalCollected,
            totalBets,
            avgBet,
            eventDetails
        );
    }

    public async Task<List<DailyCollectionDto>> GetDailyCollectionAsync(int year, int month)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var data = await (
            from e in _db.LotteryEvents
            join b in _db.Bets on e.Id equals b.EventId
            where e.EventDate >= startDate && e.EventDate <= endDate
            group b by e.EventDate into g
            select new DailyCollectionDto(
                g.Key,
                g.Key.Day,
                g.Sum(b => b.Amount),
                g.Count()
            )
        ).ToListAsync();

        // Llenar días sin datos con ceros
        var allDays = Enumerable.Range(1, endDate.Day)
            .Select(day => new DateOnly(year, month, day))
            .ToList();

        var result = allDays.Select(date =>
        {
            var existing = data.FirstOrDefault(d => d.Date == date);
            return existing ?? new DailyCollectionDto(date, date.Day, 0, 0);
        }).ToList();

        return result;
    }

    public async Task<List<TopWinnerDto>> GetTopWinnersByFrequencyAsync(
        int year, int month, int top = 10)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var winners = await (
            from b in _db.Bets
            join e in _db.LotteryEvents on b.EventId equals e.Id
            join c in _db.Customers on b.CustomerId equals c.Id
            join p in _db.Payouts on b.Id equals p.BetId
            where e.EventDate >= startDate && e.EventDate <= endDate &&
                  e.WinningNumber.HasValue &&
                  b.NumberPlayed == e.WinningNumber.Value
            group p by new { b.CustomerId, c.FullName } into g
            orderby g.Count() descending
            select new TopWinnerDto(
                g.Key.CustomerId,
                g.Key.FullName,
                g.Count(),
                g.Sum(p => p.CalculatedPrize),
                g.Average(p => p.CalculatedPrize)
            )
        ).Take(top).ToListAsync();

        return winners;
    }

    public async Task<List<TopWinnerDto>> GetTopWinnersByAmountAsync(
        int year, int month, int top = 10)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var winners = await (
            from b in _db.Bets
            join e in _db.LotteryEvents on b.EventId equals e.Id
            join c in _db.Customers on b.CustomerId equals c.Id
            join p in _db.Payouts on b.Id equals p.BetId
            where e.EventDate >= startDate && e.EventDate <= endDate &&
                  e.WinningNumber.HasValue &&
                  b.NumberPlayed == e.WinningNumber.Value
            group p by new { b.CustomerId, c.FullName } into g
            orderby g.Sum(p => p.CalculatedPrize) descending
            select new TopWinnerDto(
                g.Key.CustomerId,
                g.Key.FullName,
                g.Count(),
                g.Sum(p => p.CalculatedPrize),
                g.Average(p => p.CalculatedPrize)
            )
        ).Take(top).ToListAsync();

        return winners;
    }

    public async Task<DailySummaryDto> GetDailySummaryAsync(DateOnly date)
    {
        var events = await _db.LotteryEvents
            .Where(e => e.EventDate == date)
            .ToListAsync();

        var eventIds = events.Select(e => e.Id).ToList();

        var bets = await _db.Bets
            .Where(b => eventIds.Contains(b.EventId))
            .ToListAsync();

        var payouts = await (
            from p in _db.Payouts
            join b in _db.Bets on p.BetId equals b.Id
            where eventIds.Contains(b.EventId)
            select p
        ).ToListAsync();

        var lotteryTypes = await _db.LotteryTypes.ToListAsync();

        var byLotteryType = lotteryTypes.Select(lt =>
        {
            var ltEvents = events.Where(e => e.LotteryTypeId == lt.Id).ToList();
            var ltEventIds = ltEvents.Select(e => e.Id).ToList();
            var ltBets = bets.Where(b => ltEventIds.Contains(b.EventId)).ToList();
            var ltPayouts = payouts.Where(p => ltBets.Any(b => b.Id == p.BetId)).ToList();

            return new LotteryTypeSummaryDto(
                lt.Name,
                ltBets.Sum(b => b.Amount),
                ltBets.Count,
                ltPayouts.Count,
                ltPayouts.Sum(p => p.CalculatedPrize)
            );
        }).Where(x => x.Bets > 0).ToList();

        return new DailySummaryDto(
            date,
            bets.Sum(b => b.Amount),
            bets.Count,
            events.Count,
            events.Count(e => e.State == EventState.RESULTS_PUBLISHED),
            payouts.Count,
            payouts.Sum(p => p.CalculatedPrize),
            byLotteryType
        );
    }

    public async Task<LotteryTypeDetailDto> GetLotteryTypeDetailAsync(
        ulong lotteryTypeId, DateOnly startDate, DateOnly endDate)
    {
        var lotteryType = await _db.LotteryTypes.FindAsync(lotteryTypeId);
        if (lotteryType == null)
            throw new Exception("Tipo de lotería no encontrado");

        var events = await _db.LotteryEvents
            .Where(e => e.LotteryTypeId == lotteryTypeId &&
                       e.EventDate >= startDate &&
                       e.EventDate <= endDate)
            .ToListAsync();

        var eventIds = events.Select(e => e.Id).ToList();

        var bets = await _db.Bets
            .Where(b => eventIds.Contains(b.EventId))
            .ToListAsync();

        var payouts = await (
            from p in _db.Payouts
            join b in _db.Bets on p.BetId equals b.Id
            where eventIds.Contains(b.EventId)
            select p
        ).ToListAsync();

        var byEventNumber = events
            .GroupBy(e => e.EventNumberOfDay)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var eventNums = g.Select(e => e.Id).ToList();
                    var numBets = bets.Where(b => eventNums.Contains(b.EventId)).ToList();
                    return new EventNumberStatsDto(
                        g.Key,
                        numBets.Sum(b => b.Amount),
                        numBets.Count,
                        g.Count()
                    );
                }
            );

        return new LotteryTypeDetailDto(
            lotteryType.Id,
            lotteryType.Name,
            startDate,
            endDate,
            bets.Sum(b => b.Amount),
            bets.Count,
            events.Count,
            payouts.Count,
            payouts.Sum(p => p.CalculatedPrize),
            byEventNumber
        );
    }
}