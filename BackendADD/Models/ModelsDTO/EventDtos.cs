namespace BackendADD.Models.ModelsDTO


{
    public class EventDtos
    {
        public record WinnerDto(
    ulong BetId,
    string CustomerName,
    byte ChosenNumber,
    decimal BetAmount,
    decimal BasePrize,
    decimal BirthdayBonus,
    decimal TotalPrize,
    bool IsBirthday
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

        public record PublishResultsDto(byte WinningNumber);

        public record PublishResultsResponseDto(
            ulong EventId,
            byte WinningNumber,
            int WinnersCount,
            string Message
        );
    }
}
