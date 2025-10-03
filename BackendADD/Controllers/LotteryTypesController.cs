using BackendADD.Dtos;
using BackendADD.Infrastructure;
using BackendADD.Models;
using BackendADD.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BackendADD.Controllers;

[ApiController]
[Route("api/lottery-types")]
public class LotteryTypesController : ControllerBase
{
    private readonly ILotteryTypeRepository _repo;
    public LotteryTypesController(ILotteryTypeRepository repo) => _repo = repo;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LotteryType>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] bool? active)
    {
        var list = await _repo.GetAllAsync(active);
        return this.ApiOk(list, "Lista de tipos");
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<LotteryType>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 404)]
    public async Task<IActionResult> GetById(ulong id)
    {
        var t = await _repo.GetByIdAsync(id);
        if (t is null) return this.ApiNotFound("Tipo no encontrado");
        return this.ApiOk(t, "Tipo encontrado");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LotteryType>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 400)]
    public async Task<IActionResult> Create([FromBody] CreateLotteryTypeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return this.ApiBadRequest("Nombre requerido", new { field = "name" });

        if (dto.PayoutFactor <= 0 || dto.EventsPerDay <= 0)
            return this.ApiBadRequest("Valores inválidos", new { dto.PayoutFactor, dto.EventsPerDay });

        var entity = new LotteryType
        {
            Name = dto.Name.Trim(),
            PayoutFactor = dto.PayoutFactor,
            EventsPerDay = dto.EventsPerDay,
            IsActive = dto.IsActive
        };

        await _repo.AddAsync(entity);
        await _repo.SaveAsync();

        return this.ApiCreated(entity, "Tipo creado");
    }
}
