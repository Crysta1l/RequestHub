using System.Net;
using System.Net.Mail;

namespace RequestHub.Services.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // Send email notification
        public async Task SendAsync(string toEmail, string subject, string body)
        {
            var smtp = _config.GetSection("SmtpSettings");
            var host = smtp["Host"]!;
            var port = int.Parse(smtp["Port"]!);
            var email = smtp["Email"]!;
            var password = smtp["Password"]!;
            var name = smtp["DisplayName"]!;

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(email, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(email, name),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
        }

        // Status change notification
        public async Task SendStatusChangedAsync(string toEmail, string requestTitle, string oldStatus, string newStatus)
        {
            var subject = $"[RequestHub] Request status changed: {requestTitle}";
            var body = $@"
                <h2>Request Status Update</h2>
                <p>Your request <strong>{requestTitle}</strong> status has changed.</p>
                <p><strong>From:</strong> {oldStatus}</p>
                <p><strong>To:</strong> {newStatus}</p>
                <br>
                <p>Login to RequestHub to view details.</p>
            ";
            await SendAsync(toEmail, subject, body);
        }

        // Expiry warning notification
        public async Task SendExpiryWarningAsync(string toEmail, string requestTitle, DateTime expiryDate)
        {
            var subject = $"[RequestHub] Access expiring soon: {requestTitle}";
            var body = $@"
                <h2>Access Expiry Warning</h2>
                <p>Your access for <strong>{requestTitle}</strong> is expiring soon.</p>
                <p><strong>Expiry Date:</strong> {expiryDate:yyyy-MM-dd}</p>
                <br>
                <p>Login to RequestHub to renew or review your access.</p>
            ";
            await SendAsync(toEmail, subject, body);
        }
    }
}