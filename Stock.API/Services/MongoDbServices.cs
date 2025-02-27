using MongoDB.Driver;

namespace Stock.API.Services
{
    public class MongoDbServices
    {
        readonly IMongoDatabase _database;
        public MongoDbServices(IConfiguration configuration)
        {
            MongoClient client = new(configuration.GetConnectionString("MongoDB"));
            _database = client.GetDatabase("StockDB");
        }
        public IMongoCollection<T> GetCollection<T>()=> 
            _database.GetCollection<T>(typeof(T).Name.ToLowerInvariant());
    }
}