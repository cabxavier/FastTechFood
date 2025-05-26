using FastTechFood.Application.Dtos;
using FastTechFood.Domain.Entities;

namespace FastTechFood.Application.Interfaces
{
    public interface IUserService
    {
        Task RegisterCustomerAsync(RegisterCustomerDTO registerCustomerDTO);
        Task RegisterEmployeeAsync(RegisterEmployeeDTO registerEmployeeDTO);
        Task<string> LoginAsync(LoginDTO loginDTO);
        Task<UserDTO?> GetUserByIdAsync(Guid id);
        Task UpdateUserAsync(User user);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> CpfExistsAsync(string cpf);
    }
}
