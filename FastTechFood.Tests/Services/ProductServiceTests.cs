using Moq;
using FastTechFood.Domain.Interfaces;
using FastTechFood.Application.Services;
using Microsoft.Extensions.Logging;
using FastTechFood.Application.Dtos;
using FastTechFood.Domain.Enums;
using FastTechFood.Domain.Entities;
using FastTechFood.Domain.Exceptions;

namespace FastTechFood.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IProductRepository> productRepositoryMock;
        private readonly Mock<ILogger<ProductService>> loggerMock;
        private readonly ProductService productService;

        public ProductServiceTests()
        {
            this.productRepositoryMock = new Mock<IProductRepository>();
            this.loggerMock = new Mock<ILogger<ProductService>>();
            this.productService = new ProductService(this.productRepositoryMock.Object, this.loggerMock.Object);
        }

        [Fact]
        public async Task AddProductAsync_ShouldAddProduct_WhenValidData()
        {
            var productDTO = new ProductDTO(Guid.NewGuid(), "Hambúrguer Artesanal", "Delicioso hambúrguer 200g com queijo", 25.90m, true, ProductType.Sandwich);

            this.productRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Product>()))
                .Returns(Task.CompletedTask);

            await this.productService.AddProductAsync(productDTO);

            this.productRepositoryMock.Verify(x => x.AddAsync(It.Is<Product>(p =>
                p.Name == productDTO.Name &&
                p.Description == productDTO.Description &&
                p.Price == productDTO.Price &&
                p.ProductType == productDTO.ProductType)),
                Times.Once);

            this.VerifyLogMessage("Iniciando registro de novo produto Hambúrguer Artesanal");
            this.VerifyLogMessage("Produto registrado com sucesso Hambúrguer Artesanal");
        }

        [Fact]
        public async Task AddProductAsync_ShouldLogError_WhenExceptionOccurs()
        {
            var exception = new Exception("Database error");

            this.productRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Product>()))
                .ThrowsAsync(exception);

            await Assert.ThrowsAsync<DomainException>(() =>
                this.productService.AddProductAsync(new ProductDTO(Guid.NewGuid(), "Hambúrger Artesanal", "Delicioso hambúrguer 200g com queijo", 10.99m, true, ProductType.Sandwich)));

            this.VerifyLogMessage("Iniciando registro de novo produto Hambúrger Artesanal");
            this.VerifyLogMessage("Erro ao registrar o produto Hambúrger Artesanal", LogLevel.Error, exception);
        }

        [Fact]
        public async Task GetAllProductsAsync_ShouldReturnAllProducts()
        {
            this.productRepositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Product> { new Product("Hambúrguer", "Descrição 1", 20.50m, ProductType.Sandwich), new Product("Coca-Cola", "Descrição 2", 8.50m, ProductType.Drink) });

            var result = await this.productService.GetAllProductsAsync();

            Assert.Equal(2, result.Count());
            Assert.Contains(result, p => p.Name == "Hambúrguer");
            Assert.Contains(result, p => p.Name == "Coca-Cola");

            this.VerifyLogMessage("Buscando todos os produtos");
        }

        [Fact]
        public async Task GetProductsByTypeAsync_ShouldReturnFilteredProducts()
        {
            this.productRepositoryMock.Setup(x => x.GetByTypeAsync(ProductType.Sandwich))
                .ReturnsAsync(new List<Product> { new Product("Hambúrguer", "Descrição 1", 20.50m, ProductType.Sandwich), new Product("Coca-Cola", "Descrição 2", 8.50m, ProductType.Drink) }
                .Where(p => p.ProductType == ProductType.Sandwich).ToList());

            var result = await this.productService.GetProductsByTypeAsync(ProductType.Sandwich);

            Assert.Single(result);
            Assert.Equal("Hambúrguer", result.First().Name);

            this.VerifyLogMessage($"Buscando produto por tipo de produto {ProductType.Sandwich}");
        }

        [Fact]
        public async Task GetProductByIdAsync_ShouldReturnProduct_WhenExists()
        {
            var productId = Guid.NewGuid();

            this.productRepositoryMock.Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync(new Product("Hambúrguer", "Descrição", 20.50m, ProductType.Sandwich) { Id = productId });

            var result = await this.productService.GetProductByIdAsync(productId);

            Assert.NotNull(result);
            Assert.Equal(productId, result.Id);
            Assert.Equal("Hambúrguer", result.Name);

            this.VerifyLogMessage($"Buscando produto por Id {productId}");
        }

        [Fact]
        public async Task UpdateProductAsync_ShouldUpdateProduct_WhenValidData()
        {
            var productId = Guid.NewGuid();
            var productDTO = new ProductDTO(productId, "Hambúrguer Atualizado", "Nova descrição", 22.50m, true, ProductType.Sandwich);

            this.productRepositoryMock.Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync(new Product("Hambúrguer", "Descrição", 20.50m, ProductType.Sandwich) { Id = productId });

            this.productRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Product>()))
                .Returns(Task.CompletedTask);

            await this.productService.UpdateProductAsync(productId, productDTO);

            this.productRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Product>(p =>
                p.Name == productDTO.Name &&
                p.Description == productDTO.Description &&
                p.Price == productDTO.Price &&
                p.IsActive == productDTO.IsActive &&
                p.ProductType == productDTO.ProductType)),
                Times.Once);

            this.VerifyLogMessage($"Atualizando produto {productId}");
            this.VerifyLogMessage($"Produto atualizado com sucesso {productId}");
        }

        [Fact]
        public async Task UpdateProductAsync_ShouldThrowException_WhenProductNotFound()
        {
            var productId = Guid.NewGuid();

            this.productRepositoryMock.Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync((Product)null);

            await Assert.ThrowsAsync<DomainException>(() =>
                this.productService.UpdateProductAsync(productId, new ProductDTO(productId, "Hambúrguer Atualizado", "Nova descrição", 22.50m, true, ProductType.Sandwich)));

            this.VerifyLogMessage($"Produto não encontrado para atualização {productId}", LogLevel.Warning);
        }

        private void VerifyLogMessage(string expectedMessage, LogLevel level = LogLevel.Information, Exception exception = null)
        {
            this.loggerMock.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}