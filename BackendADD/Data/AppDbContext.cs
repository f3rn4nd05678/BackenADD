using Microsoft.EntityFrameworkCore;
using BackendADD.Models;
using System.Data;

namespace BackendADD.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LotteryType> LotteryTypes => Set<LotteryType>();
    public DbSet<LotteryEvent> LotteryEvents => Set<LotteryEvent>();
    public DbSet<Bet> Bets => Set<Bet>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // DateOnly/TimeOnly -> MySQL 'date'/'time'
        mb.Entity<LotteryEvent>()
          .Property(e => e.EventDate).HasColumnType("date");
        mb.Entity<LotteryEvent>()
          .Property(e => e.OpenTime).HasColumnType("time");
        mb.Entity<LotteryEvent>()
          .Property(e => e.CloseTime).HasColumnType("time");

        mb.Entity<LotteryEvent>()
          .HasIndex(e => new { e.LotteryTypeId, e.EventDate, e.EventNumberOfDay })
          .IsUnique();

        mb.Entity<Bet>()
          .HasIndex(b => new { b.EventId, b.NumberPlayed });

        mb.Entity<Bet>()
          .HasIndex(b => b.QrToken).IsUnique();

        mb.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });

        mb.Entity<AppSetting>(e =>
        {
            e.ToTable("app_settings");
            e.HasKey(x => x.K);
            e.Property(x => x.K).HasColumnName("k").HasMaxLength(100);
            e.Property(x => x.V).HasColumnName("v").HasMaxLength(255).IsRequired();
        });

    }
}
