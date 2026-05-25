using Microsoft.EntityFrameworkCore;
using RequestHub.Data;
using RequestHub.Services.Services;

namespace RequestHub.Services
{
    public class ExpiryNotificationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExpiryNotificationService> _logger;

        public ExpiryNotificationService(IServiceScopeFactory scopeFactory, ILogger<ExpiryNotificationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckExpiringRequests();

                // Check once per day
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task CheckExpiringRequests()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

            // Find requests expiring in next 3 days
            var warningDate = DateTime.UtcNow.AddDays(3);

            var expiringRequests = await context.AccessRequests
                .Where(r => r.Status == "Approved" &&
                            r.ExpiryDate.HasValue &&
                            r.ExpiryDate.Value <= warningDate &&
                            r.ExpiryDate.Value >= DateTime.UtcNow)
                .ToListAsync();

            foreach (var request in expiringRequests)
            {
                // Get user email
                var user = await context.Users.FindAsync(request.CreatedBy);
                if (user == null) continue;

                try
                {
                    await emailService.SendExpiryWarningAsync(user.Email, request.Title, request.ExpiryDate!.Value);
                    _logger.LogInformation("Expiry warning sent for request {Id} to {Email}", request.Id, user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send expiry warning for request {Id}", request.Id);
                }
            }
        }
    }
}