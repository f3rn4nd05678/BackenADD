
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
        base.OnModelCreating(mb);

        // LotteryEvent - configurar tipos de fecha y hora
        mb.Entity<LotteryEvent>(entity =>
        {
            entity.ToTable("lottery_events");
            entity.Property(e => e.EventDate).HasColumnType("date");
            entity.Property(e => e.OpenTime).HasColumnType("time");
            entity.Property(e => e.CloseTime).HasColumnType("time");
            // Convertir ENUM a string para EventState
            entity.Property(e => e.State)
                .HasConversion<string>()
                .HasMaxLength(50);
        });

        // Bet - configurar qr_token y state
        mb.Entity<Bet>(entity =>
        {
            entity.ToTable("bets");
            entity.Property(e => e.QrToken)
                .HasMaxLength(36)
                .IsRequired();
            // Convertir ENUM a string para BetState
            entity.Property(e => e.State)
                .HasConversion<string>()
                .HasMaxLength(50);
        });

        // UserRole - composite key
        mb.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(x => new { x.UserId, x.RoleId });
        });

        // AppSetting
        mb.Entity<AppSetting>(entity =>
        {
            entity.ToTable("app_settings");
            entity.HasKey(x => x.K);
            entity.Property(x => x.K).HasMaxLength(100);
            entity.Property(x => x.V).HasMaxLength(255).IsRequired();
        });
    }
}