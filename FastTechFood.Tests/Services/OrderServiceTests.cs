using Moq;
using FastTechFood.Application.Dtos;
using FastTechFood.Application.Services;
using FastTechFood.Domain.Entities;
using FastTechFood.Domain.Enums;
using FastTechFood.Domain.Exceptions;
using FastTechFood.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FastTechFood.Tests.Services
{
    public class OrderServiceTests
    {
        private readonly Mock<IOrderRepository> orderRepositoryMock;
        private readonly Mock<IProductRepository> productRepositoryMock;
        private readonly Mock<IUserRepository> userRepositoryMock;
        private readonly Mock<ILogger<OrderService>> loggerMock;
        private readonly OrderService orderService;

        public OrderServiceTests()
        {
            this.orderRepositoryMock = new Mock<IOrderRepository>();
            this.productRepositoryMock = new Mock<IProductRepository>();
            this.userRepositoryMock = new Mock<IUserRepository>();
            this.loggerMock = new Mock<ILogger<OrderService>>();

            this.orderService = new OrderService(this.orderRepositoryMock.Object, this.productRepositoryMock.Object, this.userRepositoryMock.Object, this.loggerMock.Object);
        }

        [Fact]
        public async Task CreateOrderAsync_ShouldCreateOrder_WhenValidData()
        {
            var customerId = Guid.NewGuid();
            var createOrderDto = new CreateOrderDTO(
                customerId,
                DeliveryType.Delivery,
                new List<CreateOrderItemDTO>
                {
                    new CreateOrderItemDTO(Guid.NewGuid(), 2),
                    new CreateOrderItemDTO(Guid.NewGuid(), 1)
                });

            var products = new List<Product>
            {
                new Product("Produto 1", "Descrição", 10.99m, ProductType.Food) { Id = createOrderDto.Items[0].ProductId },
                new Product("Produto 2", "Descrição", 15.50m, ProductType.Food) { Id = createOrderDto.Items[1].ProductId }
            };

            this.userRepositoryMock.Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(new User("Cliente", "cliente@email.com", "senha", UserType.Customer));

            this.productRepositoryMock.Setup(x => x.GetByIdAsync(createOrderDto.Items[0].ProductId))
                .ReturnsAsync(products[0]);

            this.productRepositoryMock.Setup(x => x.GetByIdAsync(createOrderDto.Items[1].ProductId))
                .ReturnsAsync(products[1]);

            this.orderRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            var result = await this.orderService.CreateOrderAsync(createOrderDto);

            Assert.NotNull(result);
            Assert.Equal(customerId, result.CustomerId);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(37.48m, result.Total);

            this.VerifyLogMessage($"Iniciando registro de novo pedido para o cliente: {customerId}");
            this.VerifyLogMessage($"Pedido: {result.Id} registrado com sucesso para o cliente: {customerId}");
        }

        [Fact]
        public async Task CreateOrderAsync_ShouldThrowException_WhenCustomerNotFound()
        {
            var customerId = Guid.NewGuid();

            this.userRepositoryMock.Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync((User)null);

            await Assert.ThrowsAsync<DomainException>(() => this.orderService.CreateOrderAsync(new CreateOrderDTO(customerId, DeliveryType.Delivery, new List<CreateOrderItemDTO>())));

            this.VerifyLogMessage($"Iniciando registro de novo pedido para o cliente: {customerId}");
            this.VerifyLogMessage($"Cliente não encontrado: {customerId}", LogLevel.Warning);
        }

        [Fact]
        public async Task CreateOrderAsync_ShouldThrowException_WhenUserIsNotCustomer()
        {
            var userId = Guid.NewGuid();
            var managerUser = new User("Manager", "manager@email.com", "senha", UserType.Manager);

            this.userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(managerUser);

            await Assert.ThrowsAsync<Exception>(() => this.orderService.CreateOrderAsync(new CreateOrderDTO(userId, DeliveryType.Delivery, new List<CreateOrderItemDTO>())));

            this.VerifyLogMessage($"Iniciando registro de novo pedido para o cliente: {userId}");
            this.VerifyLogMessage($"Apenas clientes podem fazer pedidos: {managerUser.UserType}", LogLevel.Warning);
        }

        [Fact]
        public async Task CreateOrderAsync_ShouldThrowException_WhenProductNotFound()
        {
            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            this.userRepositoryMock.Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(new User("Cliente", "cliente@email.com", "senha", UserType.Customer));

            this.productRepositoryMock.Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync((Product)null);

            await Assert.ThrowsAsync<DomainException>(() => this.orderService.CreateOrderAsync(new CreateOrderDTO(customerId, DeliveryType.Delivery, new List<CreateOrderItemDTO> { new CreateOrderItemDTO(productId, 1) })));

            this.VerifyLogMessage($"Iniciando registro de novo pedido para o cliente: {customerId}");
            this.VerifyLogMessage($"Produto com Id {productId} não encontrado", LogLevel.Warning);
        }

        [Fact]
        public async Task CreateOrderAsync_ShouldThrowException_WhenProductIsNotActive()
        {
            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var product = new Product("Produto Inativo", "Descrição", 10.99m, ProductType.Food) { Id = productId, IsActive = false };

            this.userRepositoryMock.Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(new User("Cliente", "cliente@email.com", "senha", UserType.Customer));

            this.productRepositoryMock.Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync(product);

            await Assert.ThrowsAsync<DomainException>(() => this.orderService.CreateOrderAsync(new CreateOrderDTO(customerId, DeliveryType.Delivery, new List<CreateOrderItemDTO> { new CreateOrderItemDTO(productId, 1) })));

            this.VerifyLogMessage($"Iniciando registro de novo pedido para o cliente: {customerId}");
            this.VerifyLogMessage($"Produto {product.Name} não está disponível", LogLevel.Warning);
        }

        [Fact]
        public async Task GetPendingOrdersAsync_ShouldReturnPendingOrders()
        {
            this.orderRepositoryMock.Setup(x => x.GetAllPendingAsync())
                .ReturnsAsync(new List<Order> { new Order(Guid.NewGuid(), DeliveryType.Delivery), new Order(Guid.NewGuid(), DeliveryType.Counter) });

            Assert.Equal(2, (await this.orderService.GetPendingOrdersAsync()).Count());
            this.VerifyLogMessage("Buscando todos os pedidos pendentes");
        }

        [Fact]
        public async Task AcceptOrderAsync_ShouldAcceptOrder_WhenValid()
        {
            var orderId = Guid.NewGuid();
            var order = new Order(Guid.NewGuid(), DeliveryType.Delivery) { Id = orderId };

            this.orderRepositoryMock.Setup(x => x.GetByIdAsync(orderId))
                .ReturnsAsync(order);

            this.orderRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            await this.orderService.AcceptOrderAsync(orderId);

            Assert.Equal(OrderStatus.Accepted, order.OrderStatus);
            this.VerifyLogMessage($"Iniciando registro de aceite do pedido: {orderId}");
            this.VerifyLogMessage($"Pedido aceite com sucesso: {orderId}");
        }

        [Fact]
        public async Task AcceptOrderAsync_ShouldThrowException_WhenOrderNotFound()
        {
            var orderId = Guid.NewGuid();

            this.orderRepositoryMock.Setup(x => x.GetByIdAsync(orderId))
                .ReturnsAsync((Order)null);

            await Assert.ThrowsAsync<DomainException>(() => this.orderService.AcceptOrderAsync(orderId));

            this.VerifyLogMessage($"Iniciando registro de aceite do pedido: {orderId}");
            this.VerifyLogMessage($"Pedido com Id {orderId} não encontrado", LogLevel.Warning);
        }

        [Fact]
        public async Task RejectOrderAsync_ShouldRejectOrder_WhenValid()
        {
            var orderId = Guid.NewGuid();
            var order = new Order(Guid.NewGuid(), DeliveryType.Delivery) { Id = orderId };

            this.orderRepositoryMock.Setup(x => x.GetByIdAsync(orderId))
                .ReturnsAsync(order);

            this.orderRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            await this.orderService.RejectOrderAsync(orderId);

            Assert.Equal(OrderStatus.Rejected, order.OrderStatus);
            this.VerifyLogMessage($"Iniciando registro de rejeição do pedido: {orderId}");
            this.VerifyLogMessage($"Pedido rejeitado com sucesso: {orderId}");
        }

        [Fact]
        public async Task CancelOrderAsync_ShouldCancelOrder_WhenValid()
        {
            var orderId = Guid.NewGuid();
            var reason = "Cliente solicitou cancelamento";
            var order = new Order(Guid.NewGuid(), DeliveryType.Delivery) { Id = orderId };

            this.orderRepositoryMock.Setup(x => x.GetByIdAsync(orderId))
                .ReturnsAsync(order);

            this.orderRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            await this.orderService.CancelOrderAsync(orderId, reason);

            Assert.Equal(OrderStatus.Canceled, order.OrderStatus);
            Assert.Equal(reason, order.CancellationReason);
            this.VerifyLogMessage($"Iniciando registro de cancelamento do pedido: {orderId}");
            this.VerifyLogMessage($"Pedido cancelado com sucesso: {orderId}");
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