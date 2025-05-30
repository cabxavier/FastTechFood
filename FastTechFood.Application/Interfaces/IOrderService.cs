using FastTechFood.Application.Dtos;

namespace FastTechFood.Application.Interfaces
{
    public interface IOrderService
    {
        Task<OrderDTO> CreateOrderAsync(CreateOrderDTO createOrderDTO);
        Task<IEnumerable<OrderDTO>> GetPendingOrdersAsync();
        Task AcceptOrderAsync(Guid orderId);
        Task RejectOrderAsync(Guid orderId);
        Task CancelOrderAsync(Guid orderId, string reason);
    }
}
