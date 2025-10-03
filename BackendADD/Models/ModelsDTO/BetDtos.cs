using BackendADD.Models;

namespace BackendADD.Dtos;

public record CreateBetDto(
    ulong EventId,
    ulong CustomerId,
    ulong UserId,
    byte NumberPlayed,
    decimal Amount
);

public record PublishResultsResponseDto(
    ulong EventId,
    byte WinningNumber,
    int WinnersCount,
    string Message
);

public record BetVoucherDto(
    ulong BetId,
    string QrToken,
    // Cliente
    string CustomerName,
    string? CustomerPhone,
    // Evento
    string LotteryTypeName,
    DateOnly EventDate,
    int EventNumberOfDay,
    // Apuesta
    byte NumberPlayed,
    decimal Amount,
    DateTime PlacedAt,
    // Operador
    string AttendedBy
);

public record BetDetailDto(
    ulong Id,
    ulong EventId,
    string LotteryTypeName,
    DateOnly EventDate,
    int EventNumberOfDay,
    string CustomerName,
    byte NumberPlayed,
    decimal Amount,
    DateTime PlacedAt,
    string QrToken,
    BetState State,
    string AttendedBy
);

public record BetResultDto(
    ulong BetId,
    string CustomerName,
    string LotteryTypeName,
    DateOnly EventDate,
    int EventNumberOfDay,
    byte NumberPlayed,
    decimal AmountBet,
    DateTime PlacedAt,
    EventState EventState,
    byte? WinningNumber,
    bool IsWinner,
    decimal? PrizeAmount,
    bool? BirthdayBonusApplied,
    DateTime? PaidAt,
    string? ReceiptNumber,
    BetState BetState
);

public record EventBetSummaryDto(
    ulong EventId,
    string LotteryTypeName,
    DateOnly EventDate,
    int EventNumberOfDay,
    decimal TotalCollected,
    int TotalBets,
    Dictionary<byte, int> BetsByNumber,
    EventState State
);

public record DailyWinnerDto(
    string LotteryTypeName,
    int EventNumberOfDay,
    byte? WinningNumber,
    string Status, // "WINNER" o "DESIERTO"
    string? CustomerName,
    decimal? PrizeAmount,
    DateTime? ResultsPublishedAt
);