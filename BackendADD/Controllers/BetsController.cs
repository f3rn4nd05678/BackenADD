using BackendADD.Dtos;
using BackendADD.Infrastructure;
using BackendADD.Models;
using BackendADD.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BackendADD.Controllers;

[ApiController]
[Route("api/bets")]
public class BetsController : ControllerBase
{
    private readonly IBetRepository _repo;
    private readonly ILotteryEventRepository _eventRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IAppSettingsRepository _settingsRepo;

    public BetsController(
        IBetRepository repo,
        ILotteryEventRepository eventRepo,
        ICustomerRepository customerRepo,
        IAppSettingsRepository settingsRepo)
    {
        _repo = repo;
        _eventRepo = eventRepo;
        _customerRepo = customerRepo;
        _settingsRepo = settingsRepo;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<Bet>>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] ulong? eventId,
        [FromQuery] ulong? customerId,
        [FromQuery] BetState? state)
    {
        var list = await _repo.GetAllAsync(eventId, customerId, state);
        return this.ApiOk(list, "Lista de apuestas");
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BetDetailDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 404)]
    public async Task<IActionResult> GetById(ulong id)
    {
        var bet = await _repo.GetByIdWithDetailsAsync(id);
        if (bet is null) return this.ApiNotFound("Apuesta no encontrada");
        return this.ApiOk(bet, "Apuesta encontrada");
    }

    [HttpGet("qr/{qrToken}")]
    [ProducesResponseType(typeof(ApiResponse<BetResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 404)]
    public async Task<IActionResult> GetByQrToken(string qrToken)
    {
        var result = await _repo.GetBetResultByQrAsync(qrToken);
        if (result is null) return this.ApiNotFound("Apuesta no encontrada");
        return this.ApiOk(result, "Resultado de apuesta");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BetVoucherDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 400)]
    public async Task<IActionResult> Create([FromBody] CreateBetDto dto)
    {
        // Validar monto mínimo
        var minBet = await _settingsRepo.GetDecimalAsync("MIN_BET_AMOUNT") ?? 1m;
        if (dto.Amount < minBet)
            return this.ApiBadRequest($"Monto mínimo es Q{minBet:F2}",
                new { field = "amount" });

        // Validar número
        if (dto.NumberPlayed > 99)
            return this.ApiBadRequest("Número debe estar entre 00 y 99",
                new { field = "numberPlayed" });

        // Verificar evento existe y está abierto
        var evt = await _eventRepo.GetByIdAsync(dto.EventId);
        if (evt is null)
            return this.ApiBadRequest("Evento no encontrado",
                new { field = "eventId" });

        if (evt.State != EventState.OPEN)
            return this.ApiBadRequest("Evento no está abierto para apuestas",
                new { state = evt.State.ToString() });

        // Verificar cliente existe
        var customer = await _customerRepo.GetByIdAsync(dto.CustomerId);
        if (customer is null)
            return this.ApiBadRequest("Cliente no encontrado",
                new { field = "customerId" });

        var bet = new Bet
        {
            EventId = dto.EventId,
            CustomerId = dto.CustomerId,
            UserId = dto.UserId,
            NumberPlayed = dto.NumberPlayed,
            Amount = dto.Amount,
            QrToken = Guid.NewGuid().ToString(), // Generar aquí en C#
            State = BetState.ISSUED
        };

        await _repo.AddAsync(bet);
        await _repo.SaveAsync();

        // Crear voucher
        var voucher = await _repo.GetVoucherDataAsync(bet.Id);

        return this.ApiCreated(voucher, "Apuesta registrada exitosamente");
    }

    [HttpGet("event/{eventId}/summary")]
    [ProducesResponseType(typeof(ApiResponse<EventBetSummaryDto>), 200)]
    public async Task<IActionResult> GetEventSummary(ulong eventId)
    {
        var summary = await _repo.GetEventSummaryAsync(eventId);
        return this.ApiOk(summary, "Resumen del evento");
    }

    [HttpGet("winners/daily")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DailyWinnerDto>>), 200)]
    public async Task<IActionResult> GetDailyWinners([FromQuery] DateOnly? date)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var winners = await _repo.GetDailyWinnersAsync(targetDate);
        return this.ApiOk(winners, "Ganadores del día");
    }
}