using BackendADD.Dtos;
using BackendADD.Infrastructure;
using BackendADD.Models;
using BackendADD.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BackendADD.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _repo;

    public CustomersController(ICustomerRepository repo) => _repo = repo;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<Customer>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var list = await _repo.GetAllAsync(search);
        return this.ApiOk(list, "Lista de clientes");
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Customer>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 404)]
    public async Task<IActionResult> GetById(ulong id)
    {
        var customer = await _repo.GetByIdAsync(id);
        if (customer is null) return this.ApiNotFound("Cliente no encontrado");
        return this.ApiOk(customer, "Cliente encontrado");
    }

    [HttpGet("search-by-phone/{phone}")]
    [ProducesResponseType(typeof(ApiResponse<Customer>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 404)]
    public async Task<IActionResult> GetByPhone(string phone)
    {
        var customer = await _repo.GetByPhoneAsync(phone);
        if (customer is null) return this.ApiNotFound("Cliente no encontrado");
        return this.ApiOk(customer, "Cliente encontrado");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Customer>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 400)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return this.ApiBadRequest("Nombre completo requerido", new { field = "fullName" });

        var entity = new Customer
        {
            FullName = dto.FullName.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            BirthDate = dto.BirthDate,
            Address = dto.Address?.Trim()
        };

        await _repo.AddAsync(entity);
        await _repo.SaveAsync();

        return this.ApiCreated(entity, "Cliente creado exitosamente");
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Customer>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 404)]
    public async Task<IActionResult> Update(ulong id, [FromBody] UpdateCustomerDto dto)
    {
        var customer = await _repo.GetByIdAsync(id);
        if (customer is null) return this.ApiNotFound("Cliente no encontrado");

        customer.FullName = dto.FullName.Trim();
        customer.Phone = dto.Phone?.Trim();
        customer.Email = dto.Email?.Trim();
        customer.BirthDate = dto.BirthDate;
        customer.Address = dto.Address?.Trim();

        await _repo.UpdateAsync(customer);
        await _repo.SaveAsync();

        return this.ApiOk(customer, "Cliente actualizado");
    }

    [HttpGet("{id}/is-birthday-today")]
    [ProducesResponseType(typeof(ApiResponse<BirthdayCheckDto>), 200)]
    public async Task<IActionResult> IsBirthdayToday(ulong id)
    {
        var customer = await _repo.GetByIdAsync(id);
        if (customer is null) return this.ApiNotFound("Cliente no encontrado");

        var isBirthday = customer.BirthDate.HasValue &&
                        customer.BirthDate.Value.Month == DateTime.Today.Month &&
                        customer.BirthDate.Value.Day == DateTime.Today.Day;

        var result = new BirthdayCheckDto(customer.Id, customer.FullName, isBirthday);
        return this.ApiOk(result, isBirthday ? "¡Es su cumpleaños!" : "No es su cumpleaños");
    }
}