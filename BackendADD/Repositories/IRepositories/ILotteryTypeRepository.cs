using BackendADD.Models;

namespace BackendADD.Repositories;

public interface ILotteryTypeRepository
{
    Task<List<LotteryType>> GetAllAsync(bool? onlyActive = null);
    Task<LotteryType?> GetByIdAsync(ulong id);
    Task<LotteryType> AddAsync(LotteryType entity);
    Task UpdateAsync(LotteryType entity);
    Task SaveAsync();
}
