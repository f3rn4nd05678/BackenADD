using BackendADD.Data;
using BackendADD.Dtos;
using BackendADD.Infrastructure;
using BackendADD.Models;
using BackendADD.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendADD.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _repo;
    private readonly AppDbContext _db; 

    public CustomersController(ICustomerRepository repo, AppDbContext db)
    {
        _repo = repo;
        _db = db;
    }

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
            Address = dto.Address?.Trim(),
            IsActive = true
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

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 400)]
    public async Task<IActionResult> Delete(ulong id)
    {
        var customer = await _repo.GetByIdAsync(id);
        if (customer is null)
            return this.ApiNotFound("Cliente no encontrado");


        var hasActiveBets = await _db.Bets
            .AnyAsync(b => b.CustomerId == id && b.State != BetState.EXPIRED);

        if (hasActiveBets)
        {
            return this.ApiBadRequest(
                "No se puede eliminar el cliente porque tiene apuestas activas",
                new { customerId = id }
            );
        }

  
        customer.IsActive = false;
        await _repo.UpdateAsync(customer);
        await _repo.SaveAsync();

        return this.ApiOk<object?>(null, "Cliente deshabilitado exitosamente");
    }


    [HttpPut("{id}/reactivate")]
    [ProducesResponseType(typeof(ApiResponse<Customer>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object?>), 404)]
    public async Task<IActionResult> Reactivate(ulong id)
    {
        var customer = await _repo.GetByIdAsync(id);
        if (customer is null)
            return this.ApiNotFound("Cliente no encontrado");

        customer.IsActive = true;
        await _repo.UpdateAsync(customer);
        await _repo.SaveAsync();

        return this.ApiOk(customer, "Cliente reactivado exitosamente");
    }


    [HttpGet("inactive")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<Customer>>), 200)]
    public async Task<IActionResult> GetInactive()
    {
        var inactiveCustomers = await _db.Customers
            .Where(c => c.IsActive == false)
            .OrderBy(c => c.FullName)
            .ToListAsync();

        return this.ApiOk(inactiveCustomers, "Clientes inactivos");
    }
}