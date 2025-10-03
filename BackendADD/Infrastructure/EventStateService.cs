using BackendADD.Data;
using BackendADD.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendADD.Services;

public class EventStateService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<EventStateService> _logger;

    public EventStateService(
        IServiceProvider services,
        ILogger<EventStateService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventStateService iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await ProcessEventStates(db);
                await ProcessExpiredBets(db);

                // Ejecutar cada 5 minutos
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en EventStateService");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task ProcessEventStates(AppDbContext db)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);

        // Abrir eventos programados que ya pasaron su hora de apertura
        var toOpen = await db.LotteryEvents
            .Where(e => e.EventDate == today &&
                       e.State == EventState.PROGRAMMED &&
                       e.OpenTime <= currentTime)
            .ToListAsync();

        foreach (var evt in toOpen)
        {
            evt.State = EventState.OPEN;
            _logger.LogInformation(
                "Evento {EventId} abierto automáticamente", evt.Id);
        }

        // Cerrar eventos abiertos que ya pasaron su hora de cierre
        var toClose = await db.LotteryEvents
            .Where(e => e.EventDate == today &&
                       e.State == EventState.OPEN &&
                       e.CloseTime <= currentTime)
            .ToListAsync();

        foreach (var evt in toClose)
        {
            evt.State = EventState.CLOSED;
            _logger.LogInformation(
                "Evento {EventId} cerrado automáticamente", evt.Id);
        }

        if (toOpen.Any() || toClose.Any())
        {
            await db.SaveChangesAsync();
        }
    }

    private async Task ProcessExpiredBets(AppDbContext db)
    {
        // Obtener configuración de días para reclamar premio
        var claimDaysSetting = await db.AppSettings
            .FirstOrDefaultAsync(s => s.K == "PRIZE_CLAIM_BUSINESS_DAYS");

        var claimDays = claimDaysSetting != null &&
            int.TryParse(claimDaysSetting.V, out var days) ? days : 5;

        // Buscar apuestas ganadoras pendientes de pago que ya expiraron
        var expiredBets = await (
            from b in db.Bets
            join e in db.LotteryEvents on b.EventId equals e.Id
            where b.State == BetState.WIN_PENDING &&
                  e.State == EventState.RESULTS_PUBLISHED &&
                  e.ResultsPublishedAt.HasValue &&
                  e.ResultsPublishedAt.Value.AddDays(claimDays) < DateTime.UtcNow
            select b
        ).ToListAsync();

        foreach (var bet in expiredBets)
        {
            bet.State = BetState.EXPIRED;
            _logger.LogInformation(
                "Apuesta {BetId} marcada como expirada", bet.Id);
        }

        if (expiredBets.Any())
        {
            await db.SaveChangesAsync();
        }
    }
}