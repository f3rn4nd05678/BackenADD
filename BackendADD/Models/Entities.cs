namespace BackendADD.Models;

public class Role
{
    public ulong Id { get; set; }
    public string Name { get; set; } = null!;
}
public class User
{
    public ulong Id { get; set; }
    public string Username { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = ""; // placeholder
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class UserRole
{
    public ulong UserId { get; set; }
    public ulong RoleId { get; set; }
}

public class Customer
{
    public ulong Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class LotteryType
{
    public ulong Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal PayoutFactor { get; set; }
    public int EventsPerDay { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum EventState { PROGRAMMED, OPEN, CLOSED, RESULTS_PUBLISHED }

public class LotteryEvent
{
    public ulong Id { get; set; }
    public ulong LotteryTypeId { get; set; }
    public DateOnly EventDate { get; set; }
    public int EventNumberOfDay { get; set; }
    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }
    public EventState State { get; set; } = EventState.PROGRAMMED;
    public byte? WinningNumber { get; set; }
    public DateTime? ResultsPublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum BetState { ISSUED, WIN_PENDING, PAID, EXPIRED, VOID }

public class Bet
{
    public ulong Id { get; set; }
    public ulong EventId { get; set; }
    public ulong CustomerId { get; set; }
    public ulong UserId { get; set; }
    public byte NumberPlayed { get; set; }   // 0..99
    public decimal Amount { get; set; }
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
    public string QrToken { get; set; } = Guid.NewGuid().ToString();
    public BetState State { get; set; } = BetState.ISSUED;
}

public class Payout
{
    public ulong Id { get; set; }
    public ulong BetId { get; set; }
    public decimal CalculatedPrize { get; set; }
    public bool BirthdayBonusApplied { get; set; }
    public DateTime? PaidAt { get; set; }
    public ulong? PaidByUserId { get; set; }
    public string? ReceiptNumber { get; set; }
}

public class AuditLog
{
    public ulong Id { get; set; }
    public ulong? UserId { get; set; }
    public string Action { get; set; } = null!;
    public string Entity { get; set; } = null!;
    public ulong EntityId { get; set; }
    public string? Payload { get; set; } // JSON string
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


