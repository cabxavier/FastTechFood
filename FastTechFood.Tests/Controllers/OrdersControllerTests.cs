using FastTechFood.API.Controllers;
using FastTechFood.Application.Dtos;
using FastTechFood.Application.Interfaces;
using FastTechFood.Domain.Enums;
using FastTechFood.Messaging.Publishers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;

namespace FastTechFood.API.Tests.Controllers
{
    public class OrdersControllerTests
    {
        private readonly Mock<IOrderService> orderServiceMock;
        private readonly Mock<IRabbitMQPublisherService> rabbitMQPublisherServiceMock;
        private readonly OrdersController controller;

        public OrdersControllerTests()
        {
            this.orderServiceMock = new Mock<IOrderService>();
            this.rabbitMQPublisherServiceMock = new Mock<IRabbitMQPublisherService>();
            this.controller = new OrdersController(this.orderServiceMock.Object, this.rabbitMQPublisherServiceMock.Object);
        }

        [Fact]
        public async Task Create_WithValidDto_ShouldReturnOk()
        {
            var queueName = "queue-create-order";

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "RabbitMQ:QueueCreateOrder", queueName } }).Build();

            var createOrderDto = new CreateOrderDTO
            {
                CustomerId = Guid.NewGuid(),
                DeliveryType = DeliveryType.Delivery,
                Items = new List<CreateOrderItemDTO>
                {
                    new CreateOrderItemDTO { ProductId = Guid.NewGuid(), Quantity = 2 }
                }
            };

            this.SetupUserWithRole("Customer");

            this.rabbitMQPublisherServiceMock.Setup(x => x.GetConfiguration()).Returns(configuration);

            this.rabbitMQPublisherServiceMock.Setup(x => x.SendMessageAsync(It.IsAny<CreateOrderDTO>(), queueName)).Returns(Task.CompletedTask);

            Assert.Equal(createOrderDto, (Assert.IsType<OkObjectResult>(await this.controller.Create(createOrderDto))).Value);
        }

        [Fact]
        public async Task Create_WhenUserIsNotCustomer_ShouldReturnForbid()
        {
            this.SetupUserWithoutRole("Customer");

            Assert.IsType<ForbidResult>(await this.controller.Create(new CreateOrderDTO()));
        }

        [Fact]
        public async Task Create_WithNullDto_ShouldReturnBadRequest()
        {
            this.SetupUserWithRole("Customer");

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(await this.controller.Create(null));

            Assert.Equal("Informe os dados do pedido", badRequestResult.Value);
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

            Assert.Equal(pendingOrders, Assert.IsType<OkObjectResult>(await this.controller.GetPendingOrders()).Value);
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

            Assert.Equal("Pedido aceito com sucesso", Assert.IsType<OkObjectResult>(await this.controller.AcceptOrder(orderId)).Value);
        }

        [Fact]
        public async Task AcceptOrder_WithInvalidId_ShouldReturnBadRequest()
        {
            var orderId = Guid.NewGuid();

            this.orderServiceMock.Setup(x => x.AcceptOrderAsync(orderId))
                .ThrowsAsync(new Exception("Pedido não encontrado"));

            this.SetupUserWithRole("KitchenStaff");

            Assert.Equal("Pedido não encontrado", Assert.IsType<BadRequestObjectResult>(await this.controller.AcceptOrder(orderId)).Value);
        }

        [Fact]
        public async Task RejectOrder_WithValidId_ShouldReturnOk()
        {
            var orderId = Guid.NewGuid();

            this.orderServiceMock.Setup(x => x.RejectOrderAsync(orderId))
                .Returns(Task.CompletedTask);

            this.SetupUserWithRole("KitchenStaff");

            Assert.Equal("Pedido rejeitado com sucesso", Assert.IsType<OkObjectResult>(await this.controller.RejectOrder(orderId)).Value);
        }

        [Fact]
        public async Task RejectOrder_WithInvalidId_ShouldReturnBadRequest()
        {
            var orderId = Guid.NewGuid();

            this.orderServiceMock.Setup(x => x.RejectOrderAsync(orderId))
                .ThrowsAsync(new Exception("Pedido não encontrado"));

            this.SetupUserWithRole("KitchenStaff");

            Assert.Equal("Pedido não encontrado", Assert.IsType<BadRequestObjectResult>(await this.controller.RejectOrder(orderId)).Value);
        }

        [Fact]
        public async Task CancelOrder_WithValidIdAndReason_ShouldReturnOk()
        {
            var orderId = Guid.NewGuid();
            var reason = "Mudança de planos";

            this.orderServiceMock.Setup(x => x.CancelOrderAsync(orderId, reason))
                .Returns(Task.CompletedTask);

            this.SetupUserWithRole("Customer");

            Assert.Equal("Pedido cancelado com sucesso", Assert.IsType<OkObjectResult>(await this.controller.CancelOrder(orderId, reason)).Value);
        }

        [Fact]
        public async Task CancelOrder_WithInvalidId_ShouldReturnBadRequest()
        {
            var orderId = Guid.NewGuid();
            var reason = "Mudança de planos";

            this.orderServiceMock.Setup(x => x.CancelOrderAsync(orderId, reason))
                .ThrowsAsync(new Exception("Pedido não encontrado"));

            this.SetupUserWithRole("Customer");

            Assert.Equal("Pedido não encontrado", Assert.IsType<BadRequestObjectResult>(await this.controller.CancelOrder(orderId, reason)).Value);
        }

        [Fact]
        public async Task CancelOrder_WithoutCustomerRole_ShouldReturnForbid()
        {
            this.SetupUserWithoutRole("Customer");

            Assert.IsType<ForbidResult>(await this.controller.CancelOrder(Guid.NewGuid(), "Changed my mind"));
        }

        private void SetupUserWithRole(string role)
        {
            this.controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim(ClaimTypes.Name, "testuser"), new Claim(ClaimTypes.Role, role) }, "Test"))
                }
            };
        }

        private void SetupUserWithoutRole(string roleToExclude)
        {
            this.controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth")) }
            };
        }
    }
}