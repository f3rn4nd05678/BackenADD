namespace BackendADD.Dtos;

public record CalculatePayoutDto(ulong BetId);

public record CalculatedPayoutDto(
    ulong BetId,
    decimal BetAmount,
    decimal PayoutFactor,
    decimal BasePrize,
    bool IsBirthday,
    decimal BirthdayBonus,
    decimal TotalPrize
);

public record ProcessPayoutDto(
    ulong BetId,
    ulong PaidByUserId
);

public record PendingPayoutDto(
    ulong BetId,
    string CustomerName,
    string LotteryTypeName,
    DateOnly EventDate,
    int EventNumberOfDay,
    byte NumberPlayed,
    decimal BetAmount,
    decimal PrizeAmount,
    bool BirthdayBonusApplied,
    DateTime ResultsPublishedAt,
    DateTime ExpirationDate,
    int DaysRemaining
);