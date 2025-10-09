using BackendADD.Data;
using BackendADD.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendADD.Repositories;

public interface ICustomerRepository
{
    Task<List<Customer>> GetAllAsync(string? search = null);
    Task<Customer?> GetByIdAsync(ulong id);
    Task<Customer?> GetByPhoneAsync(string phone);
    Task<Customer> AddAsync(Customer entity);
    Task UpdateAsync(Customer entity);
    Task SaveAsync();
}

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;

    public CustomerRepository(AppDbContext db) => _db = db;

    public async Task<List<Customer>> GetAllAsync(string? search = null)
    {
        var q = _db.Customers
            .Where(c => c.IsActive == true) 
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            q = q.Where(c => c.FullName.ToLower().Contains(search)
                          || (c.Phone != null && c.Phone.Contains(search))
                          || (c.Email != null && c.Email.ToLower().Contains(search)));
        }

        return await q.OrderBy(c => c.FullName).ToListAsync();
    }

    public Task<Customer?> GetByIdAsync(ulong id)
        => _db.Customers.FindAsync(id).AsTask();

    public async Task<Customer?> GetByPhoneAsync(string phone)
    {
        return await _db.Customers
            .Where(c => c.IsActive == true && c.Phone == phone)
            .FirstOrDefaultAsync();
    }

    public async Task<Customer> AddAsync(Customer entity)
    {
        await _db.Customers.AddAsync(entity);
        return entity;
    }

    public Task UpdateAsync(Customer entity)
    {
        _db.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task SaveAsync() => _db.SaveChangesAsync();
}