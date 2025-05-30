using MongoDB.Bson;
using MongoDB.Driver;

namespace FastTechFood.Infrastructure.Migrations
{
    public class MongoDbMigrationRunner
    {
        private readonly IMongoDatabase _database;

        public MongoDbMigrationRunner(IMongoDatabase database)
        {
            _database = database;
        }

        public async Task RunMigrationsAsync()
        {
            await CreateCollectionsAsync();
            await CreateIndexesAsync();
        }

        private async Task CreateCollectionsAsync()
        {
            var collections = await (await _database.ListCollectionNamesAsync()).ToListAsync();

            if (!collections.Contains("Users"))
            {
                await _database.CreateCollectionAsync("Users");
                Console.WriteLine("Coleção Users criada com sucesso.");
            }

            if (!collections.Contains("Products"))
            {
                await _database.CreateCollectionAsync("Products");
                Console.WriteLine("Coleção Products criada com sucesso.");
            }

            if (!collections.Contains("Orders"))
            {
                await _database.CreateCollectionAsync("Orders");
                Console.WriteLine("Coleção Orders criada com sucesso.");
            }
        }

        private async Task CreateIndexesAsync()
        {
            var usersCollection = _database.GetCollection<BsonDocument>("Users");
            var productsCollection = _database.GetCollection<BsonDocument>("Products");

            // Índice para email único
            var emailIndexModel = new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("Email"),
                new CreateIndexOptions { Unique = true });

            // Índice para CPF único (quando existir)
            var cpfIndexModel = new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("CPF"),
                new CreateIndexOptions { Unique = true, Sparse = true });

            // Índice para nome do produto único
            var productNameIndexModel = new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("Name"),
                new CreateIndexOptions { Unique = true });

            await usersCollection.Indexes.CreateManyAsync(new[] { emailIndexModel, cpfIndexModel });
            await productsCollection.Indexes.CreateOneAsync(productNameIndexModel);

            Console.WriteLine("Índices criados com sucesso.");
        }        
    }
}