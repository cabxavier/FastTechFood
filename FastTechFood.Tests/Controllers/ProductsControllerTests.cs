using Moq;
using Microsoft.AspNetCore.Mvc;
using FastTechFood.API.Controllers;
using FastTechFood.Application.Dtos;
using FastTechFood.Application.Interfaces;
using FastTechFood.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace FastTechFood.API.Tests.Controllers
{
    public class ProductsControllerTests
    {
        private readonly Mock<IProductService> productServiceMock;
        private readonly ProductsController controller;

        public ProductsControllerTests()
        {
            this.productServiceMock = new Mock<IProductService>();
            this.controller = new ProductsController(this.productServiceMock.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOkResult_WithListOfProducts()
        {
            this.productServiceMock.Setup(x => x.GetAllProductsAsync())
                .ReturnsAsync(new List<ProductDTO> { new ProductDTO { Id = Guid.NewGuid(), Name = "Product 1" }, new ProductDTO { Id = Guid.NewGuid(), Name = "Product 2" } });

            Assert.Equal(2, Assert.IsType<List<ProductDTO>>(Assert.IsType<OkObjectResult>(await this.controller.GetAll()).Value).Count);
        }

        [Fact]
        public async Task GetAll_ReturnsEmptyList_WhenNoProductsExist()
        {
            this.productServiceMock.Setup(x => x.GetAllProductsAsync())
                .ReturnsAsync(new List<ProductDTO>());

            Assert.Empty(Assert.IsType<List<ProductDTO>>(Assert.IsType<OkObjectResult>(await this.controller.GetAll()).Value));
        }

        [Fact]
        public async Task GetAll_IsAllowAnonymous()
        {
            Assert.True(typeof(ProductsController).GetMethod(nameof(ProductsController.GetAll)).GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any(), "AllowAnonymous attribute should be present");
        }

        [Fact]
        public async Task GetByType_ReturnsOkResult_WithFilteredProducts()
        {
            var productType = ProductType.Food;

            this.productServiceMock.Setup(x => x.GetProductsByTypeAsync(productType))
                .ReturnsAsync(new List<ProductDTO> { new ProductDTO { Id = Guid.NewGuid(), Name = "Food Product 1", ProductType = ProductType.Food }, new ProductDTO { Id = Guid.NewGuid(), Name = "Food Product 2", ProductType = ProductType.Food } });

            var returnValue = Assert.IsType<List<ProductDTO>>(Assert.IsType<OkObjectResult>(await this.controller.GetByType(productType)).Value);
            Assert.Equal(2, returnValue.Count);
            Assert.All(returnValue, p => Assert.Equal(ProductType.Food, p.ProductType));
        }

        [Fact]
        public async Task GetByType_ReturnsEmptyList_WhenNoProductsOfTypeExist()
        {
            var productType = ProductType.Drink;
            this.productServiceMock.Setup(x => x.GetProductsByTypeAsync(productType))
                .ReturnsAsync(new List<ProductDTO>());

            Assert.Empty(Assert.IsType<List<ProductDTO>>(Assert.IsType<OkObjectResult>(await this.controller.GetByType(productType)).Value));
        }

        [Fact]
        public async Task GetByType_HasAuthorizeWithCustomerRole()
        {
            var authorizeAttributes = typeof(ProductsController).GetMethod(nameof(ProductsController.GetByType)).GetCustomAttributes(typeof(AuthorizeAttribute), true);

            Assert.True(authorizeAttributes.Any(), "Authorize attribute should be present");
            var authorizeAttribute = authorizeAttributes.First() as AuthorizeAttribute;
            Assert.Equal("Customer", authorizeAttribute.Roles);
        }

        [Fact]
        public async Task Create_ReturnsOkResult_WhenProductIsValid()
        {
            var productDto = new ProductDTO(Guid.NewGuid(), "New Product", "Description", 9.99m, true, ProductType.Food);

            this.productServiceMock.Setup(x => x.AddProductAsync(productDto))
                .Returns(Task.CompletedTask);

            Assert.Equal("Produto criado com sucesso", Assert.IsType<OkObjectResult>(await this.controller.Create(productDto)).Value);

            this.productServiceMock.Verify(x => x.AddProductAsync(
                It.Is<ProductDTO>(dto =>
                    dto.Id != Guid.Empty &&
                    dto.Name == "New Product" &&
                    dto.Description == "Description" &&
                    dto.Price == 9.99m &&
                    dto.IsActive == true &&
                    dto.ProductType == ProductType.Food
                )), Times.Once);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenRequiredFieldsAreMissing()
        {
            this.controller.ModelState.AddModelError("Name", "Nome é obrigatório");
            this.controller.ModelState.AddModelError("Description", "Descrição é obrigatório");

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(await this.controller.Create(new ProductDTO { Id = Guid.NewGuid(), Price = 10.00m, IsActive = true, ProductType = ProductType.Food }));
            var modelState = Assert.IsType<SerializableError>(badRequestResult.Value);

            Assert.True(modelState.ContainsKey("Name"));
            Assert.True(modelState.ContainsKey("Description"));

            this.productServiceMock.Verify(x => x.AddProductAsync(It.IsAny<ProductDTO>()), Times.Never);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenServiceThrowsException()
        {
            var productDto = new ProductDTO();
            var exceptionMessage = "Invalid product data";

            this.productServiceMock.Setup(x => x.AddProductAsync(productDto))
                .ThrowsAsync(new Exception(exceptionMessage));

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(await this.controller.Create(productDto));
            Assert.Equal(exceptionMessage, badRequestResult.Value);
        }

        [Fact]
        public async Task Create_HasAuthorizeWithManagerRole()
        {
            var authorizeAttributes = typeof(ProductsController).GetMethod(nameof(ProductsController.Create)).GetCustomAttributes(typeof(AuthorizeAttribute), true);

            Assert.True(authorizeAttributes.Any(), "Authorize attribute should be present");
            var authorizeAttribute = authorizeAttributes.First() as AuthorizeAttribute;
            Assert.Equal("Manager", authorizeAttribute.Roles);
        }

        [Fact]
        public async Task Create_ValidatesModelState()
        {
            var invalidProductDto = new ProductDTO { Name = "" };
            this.controller.ModelState.AddModelError("Name", "Name is required");

            Assert.IsType<BadRequestObjectResult>(await this.controller.Create(invalidProductDto));
        }

        [Fact]
        public async Task Update_ReturnsOkResult_WhenUpdateIsSuccessful()
        {
            var productId = Guid.NewGuid();
            var productDto = new ProductDTO(productId, "Updated Product", "Updated Description", 19.99m, true, ProductType.Drink);

            this.productServiceMock.Setup(x => x.UpdateProductAsync(productId, productDto))
                .Returns(Task.CompletedTask);

            Assert.Equal("Produto atualizdo com sucesso", Assert.IsType<OkObjectResult>(await this.controller.Update(productId, productDto)).Value);

            this.productServiceMock.Verify(x => x.UpdateProductAsync(
                It.Is<Guid>(id => id == productId),
                It.Is<ProductDTO>(dto =>
                    dto.Id == productId &&
                    dto.Name == "Updated Product" &&
                    dto.Description == "Updated Description" &&
                    dto.Price == 19.99m &&
                    dto.IsActive == true &&
                    dto.ProductType == ProductType.Drink
                )), Times.Once);
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenIdIsMissing()
        {
            var result = await this.controller.Update(Guid.NewGuid(), new ProductDTO { Name = "Product Without Id", Description = "Description", Price = 10.00m, IsActive = true, ProductType = ProductType.Food });

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Id é obrigatório", badRequestResult.Value.ToString());

            this.productServiceMock.Verify(x => x.UpdateProductAsync(It.IsAny<Guid>(), It.IsAny<ProductDTO>()), Times.Never);
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenIdsDontMatch()
        {
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(await this.controller.Update(Guid.NewGuid(), new ProductDTO(Guid.NewGuid(), "Product", "Description", 10.00m, true, ProductType.Food)));
            Assert.Contains("não corresponde", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenServiceThrowsException()
        {
            var productId = Guid.NewGuid();
            var productDto = new ProductDTO(productId, "Valid Product", "Valid Description", 10.00m, true, ProductType.Food);

            var exceptionMessage = "Product not found";

            this.productServiceMock.Setup(x => x.UpdateProductAsync(productId, productDto))
                .ThrowsAsync(new Exception(exceptionMessage));

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(await this.controller.Update(productId, productDto));
            Assert.Equal(exceptionMessage, badRequestResult.Value);

            this.productServiceMock.Verify(x => x.UpdateProductAsync(
                productId,
                It.Is<ProductDTO>(dto =>
                    dto.Id == productId &&
                    dto.Name == "Valid Product" &&
                    dto.Description == "Valid Description"
                )), Times.Once);
        }

        [Fact]
        public async Task Update_HasAuthorizeWithManagerRole()
        {
            var authorizeAttributes = typeof(ProductsController).GetMethod(nameof(ProductsController.Update)).GetCustomAttributes(typeof(AuthorizeAttribute), true);

            Assert.True(authorizeAttributes.Any(), "Authorize attribute should be present");
            var authorizeAttribute = authorizeAttributes.First() as AuthorizeAttribute;
            Assert.Equal("Manager", authorizeAttribute.Roles);
        }

        [Fact]
        public async Task Update_ValidatesModelState()
        {
            this.controller.ModelState.AddModelError("Name", "Name is required");

            Assert.IsType<BadRequestObjectResult>(await this.controller.Update(Guid.NewGuid(), new ProductDTO { Name = "" }));
        }

        [Fact]
        public void Controller_HasAuthorizeAttribute()
        {
            Assert.True(typeof(ProductsController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Any(), "Authorize attribute should be present at controller level");
        }
    }
}