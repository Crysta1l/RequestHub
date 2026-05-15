using RequestHub.Models;

namespace RequestHub.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);  

        Task AddAsync(User user);

        Task<User?> GetByIdAsync(int id);
    }
}
