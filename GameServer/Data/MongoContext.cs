using MongoDB.Driver;
using Data.Models;

namespace Data
{
    public class MongoContext
    {
        private readonly IMongoDatabase _db;
        public MongoContext(string connectionString)
        {
            var client = new MongoClient(connectionString);
            _db = client.GetDatabase("GameDB");
        }
        public IMongoCollection<User> Users => _db.GetCollection<User>("Users");
    }
}
