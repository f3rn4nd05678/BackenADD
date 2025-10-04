
using Microsoft.EntityFrameworkCore;
using BackendADD.Models;

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
        // NO aplicar convenciones automáticas
        // En su lugar, configurar manualmente las entidades principales

        // LotteryEvent
        mb.Entity<LotteryEvent>(entity =>
        {
            entity.ToTable("lottery_events");
            entity.Property(e => e.EventDate).HasColumnType("date").HasColumnName("event_date");
            entity.Property(e => e.OpenTime).HasColumnType("time").HasColumnName("open_time");
            entity.Property(e => e.CloseTime).HasColumnType("time").HasColumnName("close_time");
        });

        // UserRole - composite key
        mb.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });

        // AppSetting
        mb.Entity<AppSetting>(e =>
        {
            e.ToTable("app_settings");
            e.HasKey(x => x.K);
            e.Property(x => x.K).HasColumnName("k").HasMaxLength(100);
            e.Property(x => x.V).HasColumnName("v").HasMaxLength(255).IsRequired();
        });
    }
}