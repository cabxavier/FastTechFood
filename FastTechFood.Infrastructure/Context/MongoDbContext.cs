using FastTechFood.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace FastTechFood.Infrastructure.Context
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase mongoDatabase;

        public MongoDbContext(IConfiguration configuration)
        {
            var mongoClient = new MongoClient(configuration.GetConnectionString("MongoDB"));
            this.mongoDatabase = mongoClient.GetDatabase("FastTechFood");
        }

        public IMongoCollection<User> Users => this.mongoDatabase.GetCollection<User>("Users");
        public IMongoCollection<Product> Products => this.mongoDatabase.GetCollection<Product>("Products");
        public IMongoCollection<Order> Orders => this.mongoDatabase.GetCollection<Order>("Orders");
    }
}
