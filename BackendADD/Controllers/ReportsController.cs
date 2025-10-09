using BackendADD.Dtos;
using BackendADD.Infrastructure;
using BackendADD.Models;
using BackendADD.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BackendADD.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportRepository _repo;

    public ReportsController(IReportRepository repo) => _repo = repo;

    // Reporte de recaudación por período
    [HttpGet("collection")]
    [ProducesResponseType(typeof(ApiResponse<CollectionReportDto>), 200)]
    public async Task<IActionResult> GetCollectionReport(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] ulong? lotteryTypeId = null,
        [FromQuery] int? eventNumberOfDay = null)
    {
        var report = await _repo.GetCollectionReportAsync(
            startDate, endDate, lotteryTypeId, eventNumberOfDay);

        // Traducir estados al español
        var translatedDetails = report.EventDetails.Select(e => new EventCollectionDto(
            e.EventId,
            e.LotteryTypeName,
            e.EventDate,
            e.EventNumberOfDay,
            e.TotalCollected,
            e.TotalBets,
            TranslateState(e.State)
        )).ToList();

        var translatedReport = new CollectionReportDto(
            report.StartDate,
            report.EndDate,
            report.LotteryTypeName,
            report.EventNumber,
            report.TotalCollected,
            report.TotalBets,
            report.AverageBetAmount,
            translatedDetails
        );

        return this.ApiOk(translatedReport, "Reporte de recaudación");
    }

    private string TranslateState(string state)
    {
        return state switch
        {
            "PROGRAMMED" => "Programado",
            "OPEN" => "Abierto",
            "CLOSED" => "Cerrado",
            "RESULTS_PUBLISHED" => "Completado",
            _ => state
        };
    }

    // Recaudación diaria (para gráfica de barras)
    [HttpGet("daily-collection")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DailyCollectionDto>>), 200)]
    public async Task<IActionResult> GetDailyCollection(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var data = await _repo.GetDailyCollectionAsync(year, month);
        return this.ApiOk(data, "Recaudación diaria del mes");
    }

    // Top 10 ganadores del mes (por cantidad de veces)
    [HttpGet("top-winners-by-frequency")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TopWinnerDto>>), 200)]
    public async Task<IActionResult> GetTopWinnersByFrequency(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var winners = await _repo.GetTopWinnersByFrequencyAsync(year, month, 10);
        return this.ApiOk(winners, "Top 10 ganadores por frecuencia");
    }

    // Top 10 ganadores del mes (por monto ganado)
    [HttpGet("top-winners-by-amount")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TopWinnerDto>>), 200)]
    public async Task<IActionResult> GetTopWinnersByAmount(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var winners = await _repo.GetTopWinnersByAmountAsync(year, month, 10);
        return this.ApiOk(winners, "Top 10 ganadores por monto");
    }

    // Resumen general del día
    [HttpGet("daily-summary")]
    [ProducesResponseType(typeof(ApiResponse<DailySummaryDto>), 200)]
    public async Task<IActionResult> GetDailySummary([FromQuery] DateOnly? date)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var summary = await _repo.GetDailySummaryAsync(targetDate);
        return this.ApiOk(summary, "Resumen del día");
    }

    // Detalle por tipo de lotería
    [HttpGet("lottery-type-detail")]
    [ProducesResponseType(typeof(ApiResponse<LotteryTypeDetailDto>), 200)]
    public async Task<IActionResult> GetLotteryTypeDetail(
        [FromQuery] ulong lotteryTypeId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate)
    {
        var detail = await _repo.GetLotteryTypeDetailAsync(
            lotteryTypeId, startDate, endDate);
        return this.ApiOk(detail, "Detalle del tipo de lotería");
    }
}