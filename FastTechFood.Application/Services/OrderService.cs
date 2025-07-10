using FastTechFood.Application.Dtos;
using FastTechFood.Application.Interfaces;
using FastTechFood.Domain.Entities;
using FastTechFood.Domain.Enums;
using FastTechFood.Domain.Exceptions;
using FastTechFood.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FastTechFood.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository orderRepository;
        private readonly IProductRepository productRepository;
        private readonly IUserRepository userRepository;

        private readonly ILogger<OrderService> logger;

        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository, IUserRepository userRepository, ILogger<OrderService> logger)
        {
            this.orderRepository = orderRepository;
            this.productRepository = productRepository;
            this.userRepository = userRepository;
            this.logger = logger;
        }

        public async Task<OrderDTO> CreateOrderAsync(CreateOrderDTO createOrderDTO)
        {
            this.logger.LogInformation("Iniciando registro de novo pedido para o cliente: {CustomerId}", createOrderDTO.CustomerId);

            var customer = await this.userRepository.GetByIdAsync(createOrderDTO.CustomerId);

            if(customer == null)
            {
                this.logger.LogWarning("Cliente não encontrado: {CustomerId}", createOrderDTO.CustomerId);

                throw new DomainException("Cliente não encontrado");
            }

            if (customer.UserType != UserType.Customer)
            {
                this.logger.LogWarning("Apenas clientes podem fazer pedidos: {UserType}", customer.UserType);

                throw new Exception("Apenas clientes podem fazer pedidos");
            }
            try
            {

                var order = new Order(createOrderDTO.CustomerId, createOrderDTO.DeliveryType);

                foreach (var item in createOrderDTO.Items)
                {
                    var product = await this.productRepository.GetByIdAsync(item.ProductId);

                    if(product == null)
                    {
                        this.logger.LogWarning("Produto com Id {item.ProductId} não encontrado", item.ProductId);

                        throw new DomainException($"Produto com Id {item.ProductId} não encontrado");
                    }

                    if (!product.IsActive)
                    {
                        this.logger.LogWarning("Produto {product.Name} não está disponível", product.Name);

                        throw new DomainException($"Produto {product.Name} não está disponível");
                    }

                    order.AddItem(product, item.Quantity);
                }

                await this.orderRepository.AddAsync(order);

                var orderDTO = MapToOrderDTO(order);

                this.logger.LogInformation("Pedido {orderId} registrado com sucesso para o cliente: {CustomerId}", order.Id, createOrderDTO.CustomerId);         

                return orderDTO;
            }
            catch(Exception ex)
            {
                this.logger.LogError(ex, "Erro ao registrar pedido para o cliente: {CustomerId}", createOrderDTO.CustomerId);

                throw new DomainException(ex.Message);
            }            
        }

        public async Task<IEnumerable<OrderDTO>> GetPendingOrdersAsync()
        {
            this.logger.LogInformation("Buscando todos os pedidos pendentes");

            return (await this.orderRepository.GetAllPendingAsync()).Select(MapToOrderDTO);
        }

        public async Task AcceptOrderAsync(Guid orderId)
        {
            this.logger.LogInformation("Iniciando registro de aceite do pedido: {orderId}", orderId);

            var order = await this.orderRepository.GetByIdAsync(orderId);

            if(order == null)
            {
                this.logger.LogWarning("Pedido com Id {orderId} não encontrado", orderId);

                throw new DomainException("Pedido não encontrado");
            }

            try
            {
                order.Accept();

                await this.orderRepository.UpdateAsync(order);

                this.logger.LogInformation("Pedido aceite com sucesso: {orderId}", orderId);
            }
            catch(Exception ex)
            {
                this.logger.LogError(ex, "Erro ao aceitar o pedido: {orderId}", orderId);

                throw new DomainException(ex.Message);
            }
        }

        public async Task RejectOrderAsync(Guid orderId)
        {
            this.logger.LogInformation("Iniciando registro de rejeição do pedido: {orderId}", orderId);

            var order = await this.orderRepository.GetByIdAsync(orderId);

            if (order == null)
            {
                this.logger.LogWarning("Pedido com Id {orderId} não encontrado", orderId);

                throw new DomainException("Pedido não encontrado");
            }

            try
            {
                order.Reject();

                await this.orderRepository.UpdateAsync(order);

                this.logger.LogInformation("Pedido rejeitado com sucesso: {orderId}", orderId);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Erro ao rejeitar o pedido: {orderId}", orderId);

                throw new DomainException(ex.Message);
            }
        }

        public async Task CancelOrderAsync(Guid orderId, string reason)
        {
            this.logger.LogInformation("Iniciando registro de cancelamento do pedido: {orderId}", orderId);

            var order = await this.orderRepository.GetByIdAsync(orderId);

            if (order == null)
            {
                this.logger.LogWarning("Pedido com Id {orderId} não encontrado", orderId);

                throw new DomainException("Pedido não encontrado");
            }

            try
            {
                order.Cancel(reason);

                await this.orderRepository.UpdateAsync(order);

                this.logger.LogInformation("Pedido cancelado com sucesso: {orderId}", orderId);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Erro ao cancelar o pedido: {orderId}", orderId);

                throw new DomainException(ex.Message);
            }
        }

        private static OrderDTO MapToOrderDTO(Order order)
        {
            return new OrderDTO
            (
                order.Id,
                order.CustomerId,
                order.CreationDate,
                order.OrderStatus,
                order.DeliveryType,
                order.CancellationReason,
                order.Items.Select(p => new OrderItemDTO
                (
                    p.ProductId,
                    p.ProductName,
                    p.UnitPrice,
                    p.Quantity,
                    p.Total

                )).ToList(),
                order.Total
            );
        }
    }
}