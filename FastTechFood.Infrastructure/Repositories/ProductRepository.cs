using FastTechFood.Domain.Entities;
using FastTechFood.Domain.Enums;
using FastTechFood.Domain.Interfaces;
using FastTechFood.Infrastructure.Context;
using MongoDB.Driver;

namespace FastTechFood.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly MongoDbContext mongoDbContext;

        public ProductRepository(MongoDbContext mongoDbContext)
        {
            this.mongoDbContext = mongoDbContext;
        }

        public async Task AddAsync(Product product)
        {
            await this.mongoDbContext.Products.InsertOneAsync(product);
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await this.mongoDbContext.Products.Find(_ => true).ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetByTypeAsync(ProductType productType)
        {
            return await this.mongoDbContext.Products.Find(x => x.ProductType == productType).ToListAsync();
        }

        public async Task<Product> GetByIdAsync(Guid id)
        {
            return await this.mongoDbContext.Products.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            await this.mongoDbContext.Products.ReplaceOneAsync(x => x.Id == product.Id, product);
        }
    }
}
