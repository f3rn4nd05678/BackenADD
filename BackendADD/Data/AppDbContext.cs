using Microsoft.EntityFrameworkCore;
using BackendADD.Models;

namespace BackendADD.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }

    // DbSets
    public DbSet<User> Users => Set<User>();
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

        // User - configuración
        mb.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.IsActive);

            // Convertir ENUM a tinyint
            entity.Property(e => e.Role)
                .HasConversion<byte>();
        });

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