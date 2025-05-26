using FastTechFood.Application.Dtos;
using FastTechFood.Application.Interfaces;
using FastTechFood.Domain.Entities;
using FastTechFood.Domain.Enums;
using FastTechFood.Domain.Exceptions;
using FastTechFood.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FastTechFood.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository productRepository;
        private readonly ILogger<ProductService> logger;

        public ProductService(IProductRepository productRepository, ILogger<ProductService> logger)
        {
            this.productRepository = productRepository;
            this.logger = logger;
        }

        public async Task AddProductAsync(ProductDTO produtoDTO)
        {
            this.logger.LogInformation("Iniciando registro de novo produto: {Name}", produtoDTO.Name);

            try
            {
                var product = new Product(
                    produtoDTO.Name,
                    produtoDTO.Description,
                    produtoDTO.Price,
                    produtoDTO.ProductType
                    );

                await this.productRepository.AddAsync(product);

                this.logger.LogInformation("Produto registrado com sucesso: {Name}", product.Name);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Erro ao registrar produto: {Name}", produtoDTO.Name);

                throw new DomainException(ex.Message);
            }
        }

        public async Task<IEnumerable<ProductDTO?>> GetAllProductsAsync()
        {
            this.logger.LogInformation("Buscando todos os produtos");

            return (await this.productRepository.GetAllAsync()).Select(p => new ProductDTO
            (
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.IsActive,
                p.ProductType
            ));
        }

        public async Task<IEnumerable<ProductDTO?>> GetProductsByTypeAsync(ProductType productType)
        {
            this.logger.LogInformation("Buscando produto por tipo de produto: {ProductType}", productType);

            return (await this.productRepository.GetByTypeAsync(productType)).Select(p => new ProductDTO
            (
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.IsActive,
                p.ProductType
            ));
        }

        public async Task<ProductDTO?> GetProductByIdAsync(Guid id)
        {
            this.logger.LogInformation("Buscando produto por Id: {Id}", id);

            var product = await this.productRepository.GetByIdAsync(id);

            var productDTO = new ProductDTO
            (
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.IsActive,
                product.ProductType
            );

            return productDTO;
        }

        public async Task UpdateProductAsync(Guid id, ProductDTO productDTO)
        {
            this.logger.LogInformation("Atualizando produto: {Id}", id);

            var product = await this.productRepository.GetByIdAsync(id);

            if (product is null)
            {
                this.logger.LogWarning("Produto não encontrado para atualização: {Id}", id);

                throw new DomainException("Produto não encontrado");
            }
            try
            {
                product.Update(
                    productDTO.Name,
                    productDTO.Description,
                    productDTO.Price,
                    productDTO.IsActive,
                    productDTO.ProductType
                    );

                await this.productRepository.UpdateAsync(product);

                this.logger.LogInformation("Produto atualizado com sucesso: {Id}", id);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Erro ao atualizar produto: {Id}", id);

                throw new DomainException(ex.Message);
            }
        }
    }
}
