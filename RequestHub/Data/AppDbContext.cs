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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ApprovalStep → AccessRequest (cascade delete)
            modelBuilder.Entity<ApprovalStep>()
                .HasOne(a => a.Request)
                .WithMany()
                .HasForeignKey(a => a.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // ApprovalStep → User (no cascade to avoid multiple paths)
            modelBuilder.Entity<ApprovalStep>()
                .HasOne(a => a.Approver)
                .WithMany()
                .HasForeignKey(a => a.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}