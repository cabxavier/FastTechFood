using FastTechFood.Domain.Enums;

namespace FastTechFood.Application.Dtos
{
    public record RegisterEmployeeDTO(string Name, string Email, string Password, string EmployeeCode, string Role) { }
}
