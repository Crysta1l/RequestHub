using RequestHub.Models;

namespace RequestHub.Interfaces
{
    public interface IAccessRequestRepository
    {
        Task<AccessRequest?> GetByIdAsync(int id);
        Task<List<AccessRequest>> GetByUserIdAsync(int userID);
        Task AddAsync(AccessRequest request);
        Task UpdateAsync(AccessRequest request);
        Task<bool> ExistsAsync(int id);
    }
}
