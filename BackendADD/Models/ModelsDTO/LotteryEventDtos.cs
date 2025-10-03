namespace BackendADD.Dtos;

public record CreateLotteryEventDto(
    ulong LotteryTypeId,
    DateOnly EventDate,
    int EventNumberOfDay,
    TimeOnly OpenTime,
    TimeOnly CloseTime
);

public record GenerateDailyEventsDto(DateOnly? Date);

public record PublishResultsDto(byte WinningNumber);

public record EventSummaryDto(
    ulong EventId,
    string LotteryTypeName,
    DateOnly EventDate,
    int EventNumberOfDay,
    byte? WinningNumber,
    decimal TotalCollected,
    int TotalBets,
    string State
);