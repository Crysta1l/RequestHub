using Microsoft.EntityFrameworkCore;
using RequestHub.Models;

namespace RequestHub.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<RequestHistory> RequestHistories { get; set; }
        public DbSet<ApprovalStep> ApprovalSteps { get; set; }
        public DbSet<AccessRequest> AccessRequests { get; set; }
    }
}
