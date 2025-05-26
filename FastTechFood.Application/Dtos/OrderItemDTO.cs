namespace FastTechFood.Application.Dtos
{
    public record OrderItemDTO(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Total) { }
}
