using FastTechFood.Domain.Entities;

namespace FastTechFood.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(Guid id);
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByCpfAsync(string cpf);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
    }
}
