using FastTechFood.Domain.Enums;

namespace FastTechFood.Application.Dtos
{
    public record CreateOrderDTO(Guid CustomerId, DeliveryType DeliveryType, List<OrderItemRequestDTO> Items) { }
}
