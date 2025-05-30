using FastTechFood.Domain.Entities;
using FastTechFood.Domain.Enums;
using FastTechFood.Domain.Interfaces;
using FastTechFood.Infrastructure.Context;
using MongoDB.Driver;

namespace FastTechFood.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly MongoDbContext mongoDbContext;

        public OrderRepository(MongoDbContext mongoDbContext)
        {
            this.mongoDbContext = mongoDbContext;
        }

        public async Task AddAsync(Order order)
        {
            foreach (var item in order.Items)
            {
                item.ProductName = (await this.mongoDbContext.Products.Find(x => x.Id == item.ProductId).FirstOrDefaultAsync()).Name;
            }

            await this.mongoDbContext.Orders.InsertOneAsync(order);
        }

        public async Task<IEnumerable<Order>> GetAllPendingAsync()
        {
            return await this.mongoDbContext.Orders.Find(x => x.OrderStatus == OrderStatus.Pending).ToListAsync();
        }

        public async Task<Order> GetByIdAsync(Guid id)
        {
            return await this.mongoDbContext.Orders.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(Order order)
        {
            await this.mongoDbContext.Orders.ReplaceOneAsync(x => x.Id == order.Id, order);
        }
    }
}
