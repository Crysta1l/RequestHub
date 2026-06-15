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
        public DbSet<RequestFile> RequestFiles { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ApprovalStep → AccessRequest
            modelBuilder.Entity<ApprovalStep>()
                .HasOne(a => a.Request)
                .WithMany()
                .HasForeignKey(a => a.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // ApprovalStep → User
            modelBuilder.Entity<ApprovalStep>()
                .HasOne(a => a.Approver)
                .WithMany()
                .HasForeignKey(a => a.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);

            // RequestFile → AccessRequest
            modelBuilder.Entity<RequestFile>()
                .HasOne(f => f.Request)
                .WithMany()
                .HasForeignKey(f => f.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // AuditLog → User
            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification → User
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Notification → AccessRequest
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Request)
                .WithMany()
                .HasForeignKey(n => n.RequestId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}