using FastTechFood.Domain.Entities;
using FastTechFood.Domain.Interfaces;
using FastTechFood.Infrastructure.Context;
using MongoDB.Driver;

namespace FastTechFood.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly MongoDbContext mongoDbContext;

        public UserRepository(MongoDbContext mongoDbContext)
        {
            this.mongoDbContext = mongoDbContext;
        }

        public async Task AddAsync(User user)
        {
            await this.mongoDbContext.Users.InsertOneAsync(user);
        }

        public async Task<User> GetByCpfAsync(string cpf)
        {
            return await this.mongoDbContext.Users.Find(x => x.CPF == cpf).FirstOrDefaultAsync();
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await this.mongoDbContext.Users.Find(x => x.Email == email).FirstOrDefaultAsync();
        }

        public async Task<User> GetByIdAsync(Guid id)
        {
            return await this.mongoDbContext.Users.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(User user)
        {
            await this.mongoDbContext.Users.ReplaceOneAsync(x => x.Id == user.Id, user);
        }
    }
}
