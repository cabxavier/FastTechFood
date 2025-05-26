using FastTechFood.Domain.Enums;

namespace FastTechFood.Application.Dtos
{
    public record ProductDTO(Guid Id, string Name, string Description, decimal Price, bool IsActive, ProductType ProductType) { }
}
