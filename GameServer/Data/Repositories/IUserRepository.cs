using System.Threading.Tasks;
using Data.Models;
using MongoDB.Driver;

namespace Data.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByAccountAsync(string account);
        Task CreateAsync(User user);
    }

    public class UserRepository : IUserRepository
    {
        private readonly MongoContext _context;
        public UserRepository(MongoContext context) => _context = context;
        public Task<User?> GetByAccountAsync(string account)
            => _context.Users.Find(u => u.Account == account).FirstOrDefaultAsync();
        public Task CreateAsync(User user)
            => _context.Users.InsertOneAsync(user);
    }
}
