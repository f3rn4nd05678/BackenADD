namespace BackendADD.Dtos;

public record CreateLotteryTypeDto(string Name, decimal PayoutFactor, int EventsPerDay, bool IsActive = true);
public record UpdateLotteryTypeDto(string Name, decimal PayoutFactor, int EventsPerDay, bool IsActive);
public record SetActiveDto(bool IsActive);
