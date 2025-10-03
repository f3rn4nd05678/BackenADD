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

    public LotteryEventsController(
        ILotteryEventRepository repo,
        ILotteryTypeRepository typeRepo)
    {
        _repo = repo;
        _typeRepo = typeRepo;
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
            return this.ApiBadRequest("Tipo de lotería no encontrado", new { field = "lotteryTypeId" });

        if (!lotteryType.IsActive)
            return this.ApiBadRequest("El tipo de lotería no está activo", new { field = "lotteryTypeId" });

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

        evt.State = EventState.CLOSED;
        await _repo.UpdateAsync(evt);
        await _repo.SaveAsync();

        return this.ApiOk(evt, "Evento cerrado");
    }

    [HttpPut("{id}/publish-results")]
    [ProducesResponseType(typeof(ApiResponse<LotteryEvent>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 400)]
    public async Task<IActionResult> PublishResults(ulong id, [FromBody] PublishResultsDto dto)
    {
        var evt = await _repo.GetByIdAsync(id);
        if (evt is null) return this.ApiNotFound("Evento no encontrado");

        if (dto.WinningNumber < 0 || dto.WinningNumber > 99)
            return this.ApiBadRequest("Número ganador debe estar entre 00 y 99",
                new { field = "winningNumber" });

        evt.WinningNumber = dto.WinningNumber;
        evt.State = EventState.RESULTS_PUBLISHED;
        evt.ResultsPublishedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(evt);
        await _repo.SaveAsync();

        return this.ApiOk(evt, "Resultados publicados");
    }
}