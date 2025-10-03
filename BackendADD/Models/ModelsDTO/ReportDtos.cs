namespace BackendADD.Dtos;

public record CollectionReportDto(
    DateOnly StartDate,
    DateOnly EndDate,
    string? LotteryTypeName,
    int? EventNumber,
    decimal TotalCollected,
    int TotalBets,
    decimal AverageBetAmount,
    List<EventCollectionDto> EventDetails
);

public record EventCollectionDto(
    ulong EventId,
    string LotteryTypeName,
    DateOnly EventDate,
    int EventNumberOfDay,
    decimal TotalCollected,
    int TotalBets,
    string State
);

public record DailyCollectionDto(
    DateOnly Date,
    int DayOfMonth,
    decimal TotalCollected,
    int TotalBets
);

public record TopWinnerDto(
    ulong CustomerId,
    string CustomerName,
    int WinCount,
    decimal TotalWon,
    decimal AveragePrize
);

public record DailySummaryDto(
    DateOnly Date,
    decimal TotalCollected,
    int TotalBets,
    int TotalEvents,
    int EventsCompleted,
    int TotalWinners,
    decimal TotalPrizesPaid,
    List<LotteryTypeSummaryDto> ByLotteryType
);

public record LotteryTypeSummaryDto(
    string Name,
    decimal Collected,
    int Bets,
    int Winners,
    decimal PrizesPaid
);

public record LotteryTypeDetailDto(
    ulong LotteryTypeId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalCollected,
    int TotalBets,
    int TotalEvents,
    int TotalWinners,
    decimal TotalPrizesPaid,
    Dictionary<int, EventNumberStatsDto> ByEventNumber
);

public record EventNumberStatsDto(
    int EventNumber,
    decimal Collected,
    int Bets,
    int Occurrences
);