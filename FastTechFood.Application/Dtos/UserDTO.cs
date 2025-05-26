using FastTechFood.Domain.Enums;

namespace FastTechFood.Application.Dtos
{
    public record UserDTO(Guid Id, string Name, string Email, UserType UserType, string? CPF) { }
}