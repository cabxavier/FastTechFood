using FastTechFood.Application.Dtos;
using FastTechFood.Domain.Enums;

namespace FastTechFood.Application.Interfaces
{
    public interface IProductService
    {
        Task AddProductAsync(ProductDTO produtoDTO);
        Task<IEnumerable<ProductDTO?>> GetAllProductsAsync();
        Task<IEnumerable<ProductDTO?>> GetProductsByTypeAsync(ProductType productType);
        Task<ProductDTO?> GetProductByIdAsync(Guid id);
        Task UpdateProductAsync(Guid id,  ProductDTO productDTO);
    }
}
