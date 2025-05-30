using Moq;
using System.Security.Claims;
using FastTechFood.API.Controllers;
using FastTechFood.Application.Dtos;
using FastTechFood.Application.Interfaces;
using FastTechFood.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FastTechFood.API.Tests.Controllers
{
    public class OrdersControllerTests
    {
        private readonly Mock<IOrderService> orderServiceMock;
        private readonly OrdersController controller;

        public OrdersControllerTests()
        {
            this.orderServiceMock = new Mock<IOrderService>();
            this.controller = new OrdersController(this.orderServiceMock.Object);
        }

        [Fact]
        public async Task Create_WithValidDto_ShouldReturnOk()
        {
            var createOrderDto = new CreateOrderDTO
            {
                CustomerId = Guid.NewGuid(),
                DeliveryType = DeliveryType.Delivery,
                Items = new List<CreateOrderItemDTO>
                {
                    new CreateOrderItemDTO { ProductId = Guid.NewGuid(), Quantity = 2 }
                }
            };

            var expectedOrderDto = new OrderDTO(
                Id: Guid.NewGuid(),
                CustomerId: createOrderDto.CustomerId,
                CreationDate: DateTime.UtcNow,
                Status: OrderStatus.Pending,
                DeliveryType: createOrderDto.DeliveryType,
                CancellationReason: null,
                Items: new List<OrderItemDTO>(),
                Total: 0
            );

            this.orderServiceMock.Setup(x => x.CreateOrderAsync(createOrderDto))
                .ReturnsAsync(expectedOrderDto);

            this.SetupUserWithRole("Customer");

            var result = await this.controller.Create(createOrderDto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedOrderDto, okResult.Value);
        }

        [Fact]
        public async Task Create_WithNullDto_ShouldReturnBadRequest()
        {
            this.SetupUserWithRole("Customer");

            Assert.IsType<BadRequestObjectResult>(await this.controller.Create(null));
        }

        [Fact]
        public async Task Create_WithoutCustomerRole_ShouldReturnForbid()
        {
            this.controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser"), new Claim(ClaimTypes.Role, "KitchenStaff") })) }
            };

            Assert.IsType<ForbidResult>(await controller.Create(new CreateOrderDTO { CustomerId = Guid.NewGuid(), DeliveryType = DeliveryType.Delivery, Items = new List<CreateOrderItemDTO>() }));
        }

        [Fact]
        public async Task GetPendingOrders_WithAuthorizedRoles_ShouldReturnOk()
        {
            var pendingOrders = new List<OrderDTO>
            {
                new OrderDTO(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now, OrderStatus.Pending, DeliveryType.Delivery, null, new List<OrderItemDTO>(), 100m),
                new OrderDTO(Guid.NewGuid(), Guid.NewGuid(), DateTime.Now, OrderStatus.Pending, DeliveryType.DriveThru, null, new List<OrderItemDTO>(), 50m)
            };

            this.orderServiceMock.Setup(x => x.GetPendingOrdersAsync())
                .ReturnsAsync(pendingOrders);

            this.controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim(ClaimTypes.Name, "testuser"), new Claim(ClaimTypes.Role, "KitchenStaff") }, "Test")) }
            };

            var okResult = Assert.IsType<OkObjectResult>(await this.controller.GetPendingOrders());
            Assert.Equal(pendingOrders, okResult.Value);
        }

        [Fact]
        public async Task GetPendingOrders_WithoutAuthorization_ShouldReturnForbid()
        {
            this.SetupUserWithRole("Customer");

            Assert.IsType<ForbidResult>(await this.controller.GetPendingOrders());
        }

        [Fact]
        public async Task AcceptOrder_WithValidId_ShouldReturnOk()
        {
            var orderId = Guid.NewGuid();

            this.orderServiceMock.Setup(x => x.AcceptOrderAsync(orderId))
                .Returns(Task.CompletedTask);

            this.SetupUserWithRole("KitchenStaff");

            var okResult = Assert.IsType<OkObjectResult>(await this.controller.AcceptOrder(orderId));
            Assert.Equal("Pedido aceito com sucesso", okResult.Value);
        }

        [Fact]
        public async Task AcceptOrder_WithInvalidId_ShouldReturnBadRequest()
        {
            var orderId = Guid.NewGuid();

            this.orderServiceMock.Setup(x => x.AcceptOrderAsync(orderId))
                .ThrowsAsync(new Exception("Pedido não encontrado"));

            this.SetupUserWithRole("KitchenStaff");

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(await this.controller.AcceptOrder(orderId));
            Assert.Equal("Pedido não encontrado", badRequestResult.Value);
        }

        [Fact]
        public async Task RejectOrder_WithValidId_ShouldReturnOk()
        {
            var orderId = Guid.NewGuid();

            this.orderServiceMock.Setup(x => x.RejectOrderAsync(orderId))
                .Returns(Task.CompletedTask);

            this.SetupUserWithRole("KitchenStaff");

            var okResult = Assert.IsType<OkObjectResult>(await this.controller.RejectOrder(orderId));
            Assert.Equal("Pedido rejeitado com sucesso", okResult.Value);
        }

        [Fact]
        public async Task RejectOrder_WithInvalidId_ShouldReturnBadRequest()
        {
            var orderId = Guid.NewGuid();

            this.orderServiceMock.Setup(x => x.RejectOrderAsync(orderId))
                .ThrowsAsync(new Exception("Pedido não encontrado"));

            this.SetupUserWithRole("KitchenStaff");

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(await this.controller.RejectOrder(orderId));
            Assert.Equal("Pedido não encontrado", badRequestResult.Value);
        }

        [Fact]
        public async Task CancelOrder_WithValidIdAndReason_ShouldReturnOk()
        {
            var orderId = Guid.NewGuid();
            var reason = "Mudança de planos";

            this.orderServiceMock.Setup(x => x.CancelOrderAsync(orderId, reason))
                .Returns(Task.CompletedTask);

            this.SetupUserWithRole("Customer");

            var okResult = Assert.IsType<OkObjectResult>(await this.controller.CancelOrder(orderId, reason));
            Assert.Equal("Pedido cancelado com sucesso", okResult.Value);
        }

        [Fact]
        public async Task CancelOrder_WithInvalidId_ShouldReturnBadRequest()
        {
            var orderId = Guid.NewGuid();
            var reason = "Mudança de planos";

            this.orderServiceMock.Setup(x => x.CancelOrderAsync(orderId, reason))
                .ThrowsAsync(new Exception("Pedido não encontrado"));

            this.SetupUserWithRole("Customer");

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(await this.controller.CancelOrder(orderId, reason));
            Assert.Equal("Pedido não encontrado", badRequestResult.Value);
        }

        [Fact]
        public async Task CancelOrder_WithoutCustomerRole_ShouldReturnForbid()
        {
            this.SetupUserWithoutRole("Customer");

            Assert.IsType<ForbidResult>(await this.controller.CancelOrder(Guid.NewGuid(), "Changed my mind"));
        }

        private void SetupUserWithRole(string role)
        {
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim(ClaimTypes.Name, "testuser"), new Claim(ClaimTypes.Role, role) }, "Test"))
                }
            };
        }

        private void SetupUserWithoutRole(string roleToExclude)
        {
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth")) }
            };
        }
    }
}