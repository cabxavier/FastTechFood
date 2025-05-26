using FastTechFood.Domain.Enums;

namespace FastTechFood.Application.Dtos
{
    public record OrderDTO(Guid Id, Guid CustomerId, DateTime CreationDate, OrderStatus Status, DeliveryType DeliveryType,
        string? CancellationReason, List<OrderItemDTO> Items, decimal Total)
    { }
}
