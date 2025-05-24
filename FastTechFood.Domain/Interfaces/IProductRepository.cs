using FastTechFood.Domain.Entities;
using FastTechFood.Domain.Enums;

namespace FastTechFood.Domain.Interfaces
{
    public interface IProductRepository
    {
        Task<Product> GetByIdAsync(Guid id);
        Task<IEnumerable<Product>> GetAllAsync();
        Task<IEnumerable<Product>> GetByTypeAsync(ProductType productType);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
    }
}
