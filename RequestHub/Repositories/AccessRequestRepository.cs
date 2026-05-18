using Microsoft.EntityFrameworkCore;
using RequestHub.Data;
using RequestHub.Interfaces;
using RequestHub.Models;

namespace RequestHub.Repositories
{
    public class AccessRequestRepository : IAccessRequestRepository
    {
        private readonly AppDbContext _context;
        public AccessRequestRepository(AppDbContext context)
        {
            _context = context;
        }
        
        public async Task<AccessRequest?> GetByIdAsync(int id)
        {
            return await _context.AccessRequests.FindAsync(id);
        }

        public async Task<List<AccessRequest>> GetByUserIdAsync(int userId)
        {
            return await _context.AccessRequests
                .Where(r => r.CreatedBy == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(AccessRequest request)
        {
            await _context.AccessRequests.AddAsync(request);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(AccessRequest request)
        {
            _context.AccessRequests.Update(request);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> existAsync(int id)
        {
            return await _context.AccessRequests.AnyAsync(r => r.Id == id);
        }
    }
}
