using BackendADD.Dtos;
using BackendADD.Infrastructure;
using BackendADD.Models;
using BackendADD.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BackendADD.Controllers;

[ApiController]
[Route("api/lottery-events")]
public class LotteryEventsController : ControllerBase
{
    private readonly ILotteryEventRepository _repo;
    private readonly ILotteryTypeRepository _typeRepo;
    private readonly IBetRepository _betRepo;

    public LotteryEventsController(
        ILotteryEventRepository repo,
        ILotteryTypeRepository typeRepo,
        IBetRepository betRepo)
    {
        _repo = repo;
        _typeRepo = typeRepo;
        _betRepo = betRepo;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LotteryEvent>>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateOnly? date,
        [FromQuery] ulong? lotteryTypeId,
        [FromQuery] EventState? state)
    {
        var list = await _repo.GetAllAsync(date, lotteryTypeId, state);
        return this.ApiOk(list, "Lista de eventos");
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<LotteryEvent>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 404)]
    public async Task<IActionResult> GetById(ulong id)
    {
        var evt = await _repo.GetByIdAsync(id);
        if (evt is null) return this.ApiNotFound("Evento no encontrado");
        return this.ApiOk(evt, "Evento encontrado");
    }

    [HttpGet("today")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LotteryEvent>>), 200)]
    public async Task<IActionResult> GetTodayEvents()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var list = await _repo.GetAllAsync(today, null, null);
        return this.ApiOk(list, "Eventos de hoy");
    }

    [HttpGet("open")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LotteryEvent>>), 200)]
    public async Task<IActionResult> GetOpenEvents()
    {
        var list = await _repo.GetOpenEventsAsync();
        return this.ApiOk(list, "Eventos abiertos para apuestas");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LotteryEvent>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 400)]
    public async Task<IActionResult> Create([FromBody] CreateLotteryEventDto dto)
    {
        var lotteryType = await _typeRepo.GetByIdAsync(dto.LotteryTypeId);
        if (lotteryType is null)
            return this.ApiBadRequest("Tipo de lotería no encontrado",
                new { field = "lotteryTypeId" });

        if (!lotteryType.IsActive)
            return this.ApiBadRequest("El tipo de lotería no está activo",
                new { field = "lotteryTypeId" });

        var entity = new LotteryEvent
        {
            LotteryTypeId = dto.LotteryTypeId,
            EventDate = dto.EventDate,
            EventNumberOfDay = dto.EventNumberOfDay,
            OpenTime = dto.OpenTime,
            CloseTime = dto.CloseTime,
            State = EventState.PROGRAMMED
        };

        await _repo.AddAsync(entity);
        await _repo.SaveAsync();

        return this.ApiCreated(entity, "Evento creado");
    }

    [HttpPost("generate-daily")]
    [ProducesResponseType(typeof(ApiResponse<List<LotteryEvent>>), 201)]
    public async Task<IActionResult> GenerateDailyEvents([FromBody] GenerateDailyEventsDto dto)
    {
        var date = dto.Date ?? DateOnly.FromDateTime(DateTime.Today);
        var events = new List<LotteryEvent>();

        var lotteryTypes = await _typeRepo.GetAllAsync(onlyActive: true);

        foreach (var type in lotteryTypes)
        {
            for (int i = 1; i <= type.EventsPerDay; i++)
            {
                var evt = new LotteryEvent
                {
                    LotteryTypeId = type.Id,
                    EventDate = date,
                    EventNumberOfDay = i,
                    OpenTime = new TimeOnly(8, 0),
                    CloseTime = new TimeOnly(20, 0),
                    State = EventState.PROGRAMMED
                };
                await _repo.AddAsync(evt);
                events.Add(evt);
            }
        }

        await _repo.SaveAsync();
        return this.ApiCreated(events, $"Se generaron {events.Count} eventos para {date}");
    }

    [HttpPut("{id}/open")]
    [ProducesResponseType(typeof(ApiResponse<LotteryEvent>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 404)]
    public async Task<IActionResult> OpenEvent(ulong id)
    {
        var evt = await _repo.GetByIdAsync(id);
        if (evt is null) return this.ApiNotFound("Evento no encontrado");

        if (evt.State != EventState.PROGRAMMED)
            return this.ApiBadRequest("Solo eventos programados pueden abrirse",
                new { currentState = evt.State.ToString() });

        evt.State = EventState.OPEN;
        await _repo.UpdateAsync(evt);
        await _repo.SaveAsync();

        return this.ApiOk(evt, "Evento abierto para apuestas");
    }

    [HttpPut("{id}/close")]
    [ProducesResponseType(typeof(ApiResponse<LotteryEvent>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 404)]
    public async Task<IActionResult> CloseEvent(ulong id)
    {
        var evt = await _repo.GetByIdAsync(id);
        if (evt is null) return this.ApiNotFound("Evento no encontrado");

        if (evt.State != EventState.OPEN)
            return this.ApiBadRequest("Solo eventos abiertos pueden cerrarse",
                new { currentState = evt.State.ToString() });

        evt.State = EventState.CLOSED;
        await _repo.UpdateAsync(evt);
        await _repo.SaveAsync();

        return this.ApiOk(evt, "Evento cerrado");
    }

    [HttpPut("{id}/publish-results")]
    [ProducesResponseType(typeof(ApiResponse<PublishResultsResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 400)]
    public async Task<IActionResult> PublishResults(ulong id, [FromBody] PublishResultsDto dto)
    {
        var evt = await _repo.GetByIdAsync(id);
        if (evt is null) return this.ApiNotFound("Evento no encontrado");

        if (evt.State != EventState.CLOSED)
            return this.ApiBadRequest("Solo eventos cerrados pueden publicar resultados",
                new { currentState = evt.State.ToString() });

        if (dto.WinningNumber < 0 || dto.WinningNumber > 99)
            return this.ApiBadRequest("Número ganador debe estar entre 00 y 99",
                new { field = "winningNumber" });

        evt.WinningNumber = dto.WinningNumber;
        evt.State = EventState.RESULTS_PUBLISHED;
        evt.ResultsPublishedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(evt);

        // Actualizar estado de apuestas ganadoras a WIN_PENDING
        var bets = await _betRepo.GetAllAsync(eventId: id, state: BetState.ISSUED);
        int winnersCount = 0;

        foreach (var bet in bets)
        {
            if (bet.NumberPlayed == dto.WinningNumber)
            {
                bet.State = BetState.WIN_PENDING;
                await _betRepo.UpdateAsync(bet);
                winnersCount++;
            }
        }

        await _repo.SaveAsync();

        var response = new PublishResultsResponseDto(
            evt.Id,
            evt.WinningNumber.Value,
            winnersCount,
            winnersCount == 0 ? "Sorteo desierto - No hubo ganadores" : $"{winnersCount} ganador(es)"
        );

        return this.ApiOk(response, "Resultados publicados exitosamente");
    }

    [HttpGet("{id}/winners")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<WinnerDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 404)]
    public async Task<IActionResult> GetWinners(ulong id)
    {
        var evt = await _repo.GetByIdAsync(id);
        if (evt is null) return this.ApiNotFound("Evento no encontrado");

        if (evt.State != EventState.RESULTS_PUBLISHED)
            return this.ApiBadRequest<object?>("Los resultados aún no han sido publicados");

        // Obtener todas las apuestas ganadoras
        var winningBets = await _betRepo.GetAllAsync(eventId: id, state: BetState.WIN_PENDING);

        var winners = new List<WinnerDto>();

        foreach (var bet in winningBets)
        {
            var customer = await _betRepo.GetCustomerAsync(bet.CustomerId);
            if (customer is null) continue;

            // Calcular premio con bonus de cumpleaños si aplica
            var isBirthday = customer.BirthDate.HasValue &&
                customer.BirthDate.Value.Month == evt.EventDate.Month &&
                customer.BirthDate.Value.Day == evt.EventDate.Day;

            var lotteryType = await _typeRepo.GetByIdAsync(evt.LotteryTypeId);
            var basePrize = bet.Amount * (lotteryType?.PayoutFactor ?? 0);
            var bonus = isBirthday ? basePrize * 0.10m : 0;
            var totalPrize = basePrize + bonus;

            winners.Add(new WinnerDto(
                bet.Id,
                customer.FullName,
                bet.NumberPlayed,
                bet.Amount,
                basePrize,
                bonus,
                totalPrize,
                isBirthday
            ));
        }

        if (winners.Count == 0)
            return this.ApiOk(new List<WinnerDto>(), "Ganador desierto - No hubo ganadores");

        return this.ApiOk(winners, $"Se encontraron {winners.Count} ganador(es)");
    }

    [HttpGet("{id}/stats")]
    [ProducesResponseType(typeof(ApiResponse<EventStatsDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 404)]
    public async Task<IActionResult> GetStats(ulong id)
    {
        var evt = await _repo.GetByIdAsync(id);
        if (evt is null) return this.ApiNotFound("Evento no encontrado");

        // Obtener todas las apuestas del evento
        var bets = await _betRepo.GetAllAsync(eventId: id);

        var totalBets = bets.Count;
        var totalRevenue = bets.Sum(b => b.Amount);
        var uniqueCustomers = bets.Select(b => b.CustomerId).Distinct().Count();

        // Contar ganadores
        var winners = bets.Count(b => b.State == BetState.WIN_PENDING || b.State == BetState.PAID);

        // Distribución de números apostados
        var numberDistribution = bets
            .GroupBy(b => b.NumberPlayed)
            .Select(g => new NumberDistributionDto(g.Key, g.Count(), g.Sum(b => b.Amount)))
            .OrderByDescending(n => n.Count)
            .ToList();

        var stats = new EventStatsDto(
            totalBets,
            totalRevenue,
            uniqueCustomers,
            winners,
            totalBets > 0 ? totalRevenue / totalBets : 0,
            numberDistribution
        );

        return this.ApiOk(stats, "Estadísticas del evento");
    }
}