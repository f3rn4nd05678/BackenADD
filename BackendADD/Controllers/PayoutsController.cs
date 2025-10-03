using BackendADD.Dtos;
using BackendADD.Infrastructure;
using BackendADD.Models;
using BackendADD.Repositories;
using Microsoft.AspNetCore.Mvc;
using BackendADD.Services;

namespace BackendADD.Controllers;

[ApiController]
[Route("api/payouts")]
public class PayoutsController : ControllerBase
{
    private readonly IPayoutRepository _repo;
    private readonly IBetRepository _betRepo;
    private readonly ILotteryEventRepository _eventRepo;
    private readonly ILotteryTypeRepository _typeRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IAppSettingsRepository _settingsRepo;

    public PayoutsController(
        IPayoutRepository repo,
        IBetRepository betRepo,
        ILotteryEventRepository eventRepo,
        ILotteryTypeRepository typeRepo,
        ICustomerRepository customerRepo,
        IAppSettingsRepository settingsRepo)
    {
        _repo = repo;
        _betRepo = betRepo;
        _eventRepo = eventRepo;
        _typeRepo = typeRepo;
        _customerRepo = customerRepo;
        _settingsRepo = settingsRepo;
    }

    [HttpPost("calculate")]
    [ProducesResponseType(typeof(ApiResponse<CalculatedPayoutDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 400)]
    public async Task<IActionResult> CalculatePayout([FromBody] CalculatePayoutDto dto)
    {
        var bet = await _betRepo.GetByIdAsync(dto.BetId);
        if (bet is null)
            return this.ApiBadRequest("Apuesta no encontrada", new { field = "betId" });

        var evt = await _eventRepo.GetByIdAsync(bet.EventId);
        if (evt is null || evt.State != EventState.RESULTS_PUBLISHED)
            return this.ApiBadRequest("Resultados no publicados",
                new { state = evt?.State.ToString() });

        if (!evt.WinningNumber.HasValue)
            return this.ApiBadRequest<object>("Número ganador no definido");

        // Verificar si ganó
        if (bet.NumberPlayed != evt.WinningNumber.Value)
            return this.ApiBadRequest("Esta apuesta no es ganadora",
                new { played = bet.NumberPlayed, winner = evt.WinningNumber.Value });

        // Obtener tipo de lotería
        var lotteryType = await _typeRepo.GetByIdAsync(evt.LotteryTypeId);
        if (lotteryType is null)
            return this.ApiBadRequest<object>("Tipo de lotería no encontrado");

        // Calcular premio base
        var basePrize = bet.Amount * lotteryType.PayoutFactor;

        // Verificar bonificación de cumpleaños
        var customer = await _customerRepo.GetByIdAsync(bet.CustomerId);
        var isBirthday = customer?.BirthDate.HasValue == true &&
                        customer.BirthDate.Value.Month == evt.EventDate.Month &&
                        customer.BirthDate.Value.Day == evt.EventDate.Day;

        var bonusPercent = await _settingsRepo.GetDecimalAsync("BIRTHDAY_BONUS_PERCENT") ?? 10m;
        var birthdayBonus = isBirthday ? basePrize * (bonusPercent / 100) : 0m;
        var totalPrize = basePrize + birthdayBonus;

        var result = new CalculatedPayoutDto(
            bet.Id,
            bet.Amount,
            lotteryType.PayoutFactor,
            basePrize,
            isBirthday,
            birthdayBonus,
            totalPrize
        );

        return this.ApiOk(result, "Premio calculado");
    }

    [HttpPost("process")]
    [ProducesResponseType(typeof(ApiResponse<Payout>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 400)]
    public async Task<IActionResult> ProcessPayout([FromBody] ProcessPayoutDto dto)
    {
        // Verificar que no exista pago previo
        var existingPayout = await _repo.GetByBetIdAsync(dto.BetId);
        if (existingPayout != null)
            return this.ApiBadRequest("Esta apuesta ya fue pagada",
                new { paidAt = existingPayout.PaidAt });

        var bet = await _betRepo.GetByIdAsync(dto.BetId);
        if (bet is null)
            return this.ApiBadRequest<object>("Apuesta no encontrada");

        var evt = await _eventRepo.GetByIdAsync(bet.EventId);
        if (evt is null || evt.State != EventState.RESULTS_PUBLISHED)
            return this.ApiBadRequest<object>("Resultados no publicados");

        // Verificar que ganó
        if (bet.NumberPlayed != evt.WinningNumber.Value)
            return this.ApiBadRequest<object>("Esta apuesta no es ganadora");

        // Verificar plazo de reclamo (5 días hábiles)
        var claimDays = await _settingsRepo.GetIntAsync("PRIZE_CLAIM_BUSINESS_DAYS") ?? 5;
        var expirationDate = evt.ResultsPublishedAt?.AddDays(claimDays);
        if (expirationDate.HasValue && DateTime.UtcNow > expirationDate.Value)
        {
            bet.State = BetState.EXPIRED;
            await _betRepo.UpdateAsync(bet);
            await _betRepo.SaveAsync();
            return this.ApiBadRequest("Premio expirado. Plazo de reclamo vencido",
                new { expirationDate });
        }

        // Calcular premio
        var lotteryType = await _typeRepo.GetByIdAsync(evt.LotteryTypeId);
        var customer = await _customerRepo.GetByIdAsync(bet.CustomerId);

        var basePrize = bet.Amount * lotteryType!.PayoutFactor;
        var isBirthday = customer?.BirthDate.HasValue == true &&
                        customer.BirthDate.Value.Month == evt.EventDate.Month &&
                        customer.BirthDate.Value.Day == evt.EventDate.Day;

        var bonusPercent = await _settingsRepo.GetDecimalAsync("BIRTHDAY_BONUS_PERCENT") ?? 10m;
        var birthdayBonus = isBirthday ? basePrize * (bonusPercent / 100) : 0m;
        var totalPrize = basePrize + birthdayBonus;

        // Crear pago
        var payout = new Payout
        {
            BetId = dto.BetId,
            CalculatedPrize = totalPrize,
            BirthdayBonusApplied = isBirthday,
            PaidAt = DateTime.UtcNow,
            PaidByUserId = dto.PaidByUserId,
            ReceiptNumber = $"REC-{DateTime.Now:yyyyMMdd}-{bet.Id}"
        };

        await _repo.AddAsync(payout);

        // Actualizar estado de apuesta
        bet.State = BetState.PAID;
        await _betRepo.UpdateAsync(bet);

        await _repo.SaveAsync();

        return this.ApiCreated(payout, "Pago procesado exitosamente");
    }

    [HttpGet("pending")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PendingPayoutDto>>), 200)]
    public async Task<IActionResult> GetPending()
    {
        var pending = await _repo.GetPendingPayoutsAsync();
        return this.ApiOk(pending, "Pagos pendientes");
    }
}