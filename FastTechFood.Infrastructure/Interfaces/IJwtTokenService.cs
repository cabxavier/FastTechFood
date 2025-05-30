using FastTechFood.Domain.Entities;

namespace FastTechFood.Infrastructure.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user);
    }
}
