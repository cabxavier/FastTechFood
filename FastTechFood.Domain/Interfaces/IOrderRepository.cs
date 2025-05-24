using FastTechFood.Domain.Entities;

namespace FastTechFood.Domain.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order> GetByIdAsync(Guid id);
        Task<IEnumerable<Order>> GetAllPendingAsync();
        Task AddAsync(Order order);
        Task UpdateAsync(Order order);
    }
}
