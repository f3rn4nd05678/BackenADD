using BackendADD.Models;

namespace BackendADD.Dtos;

public record WinnerDto(
    ulong BetId,
    string CustomerName,
    byte ChosenNumber,
    decimal BetAmount,
    decimal BasePrize,
    decimal BirthdayBonus,
    decimal TotalPrize,
    bool IsBirthday,
    BetState State 
);

public record EventStatsDto(
    int TotalBets,
    decimal TotalRevenue,
    int UniqueCustomers,
    int TotalWinners,
    decimal AverageBetAmount,
    List<NumberDistributionDto> NumberDistribution
);

public record NumberDistributionDto(
    byte Number,
    int Count,
    decimal TotalAmount
);

