using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackendADD.Models;

[Table("roles")]
public class Role
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = null!;
}

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = null!;

    [Column("full_name")]
    public string FullName { get; set; } = null!;

    [Column("email")]
    public string? Email { get; set; }

    [Column("password_hash")]
    public string PasswordHash { get; set; } = "";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[Table("user_roles")]
public class UserRole
{
    [Column("user_id")]
    public ulong UserId { get; set; }

    [Column("role_id")]
    public ulong RoleId { get; set; }
}


[Table("customers")]
public class Customer
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("full_name")]
    public string FullName { get; set; } = null!;

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("birth_date")]
    public DateOnly? BirthDate { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
[Table("lottery_types")]
public class LotteryType
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("payout_factor")]
    public decimal PayoutFactor { get; set; }

    [Column("events_per_day")]
    public int EventsPerDay { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}

public enum EventState { PROGRAMMED, OPEN, CLOSED, RESULTS_PUBLISHED }

[Table("lottery_events")]
public class LotteryEvent
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("lottery_type_id")]
    public ulong LotteryTypeId { get; set; }

    [Column("event_date")]
    public DateOnly EventDate { get; set; }

    [Column("event_number_of_day")]
    public int EventNumberOfDay { get; set; }

    [Column("open_time")]
    public TimeOnly OpenTime { get; set; }

    [Column("close_time")]
    public TimeOnly CloseTime { get; set; }

    [Column("state")]
    public EventState State { get; set; } = EventState.PROGRAMMED;

    [Column("winning_number")]
    public byte? WinningNumber { get; set; }

    [Column("results_published_at")]
    public DateTime? ResultsPublishedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum BetState { ISSUED, WIN_PENDING, PAID, EXPIRED, VOID }

[Table("bets")]
public class Bet
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("event_id")]
    public ulong EventId { get; set; }

    [Column("customer_id")]
    public ulong CustomerId { get; set; }

    [Column("user_id")]
    public ulong UserId { get; set; }

    [Column("number_played")]
    public byte NumberPlayed { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("placed_at")]
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

    [Column("qr_token")]
    [MaxLength(36)]
    public string QrToken { get; set; } = Guid.NewGuid().ToString();

    [Column("state")]
    public BetState State { get; set; } = BetState.ISSUED;
}

[Table("payouts")]
public class Payout
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("bet_id")]
    public ulong BetId { get; set; }

    [Column("calculated_prize")]
    public decimal CalculatedPrize { get; set; }

    [Column("birthday_bonus_applied")]
    public bool BirthdayBonusApplied { get; set; }

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    [Column("paid_by_user_id")]
    public ulong? PaidByUserId { get; set; }

    [Column("receipt_number")]
    public string? ReceiptNumber { get; set; }
}

[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [Column("id")]
    public ulong Id { get; set; }

    [Column("user_id")]
    public ulong? UserId { get; set; }

    [Column("action")]
    public string Action { get; set; } = null!;

    [Column("entity")]
    public string Entity { get; set; } = null!;

    [Column("entity_id")]
    public ulong EntityId { get; set; }

    [Column("payload")]
    public string? Payload { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}