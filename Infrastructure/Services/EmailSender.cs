using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Core.Services;

namespace Infrastructure.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        private readonly MailSettingsModel _mailSettings;
        public EmailSender(ILogger<EmailSender> logger, IOptions<MailSettingsModel> mailSettings)
        {
            _logger = logger;
            _mailSettings = mailSettings.Value;
        }

        public async Task SendEmailAsync(string to, string cc, string subject, string body, string? attachments = null)
        {
            var message = new MimeMessage
            {
                Sender = MailboxAddress.Parse(_mailSettings.Email),
                Subject = subject
            };
            foreach (var address in to.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
            {
                message.To.Add(MailboxAddress.Parse(address));
            }

            foreach (var address in cc.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
            {
                message.Cc.Add(MailboxAddress.Parse(address));
            }
            var builder = new BodyBuilder();
            builder.HtmlBody = body;

            if (attachments != null)
            {
                foreach (var attachment in attachments.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (attachment.Length > 0)
                    {
                        if (File.Exists(attachment))
                        {
                            byte[] bytes = File.ReadAllBytes(attachment);
                            var memoryStream = new MemoryStream(bytes, 0, bytes.Length, true, true);
                            string fileName = Path.GetFileName(attachment);
                            builder.Attachments.Add(fileName, memoryStream);
                        }
                    }
                }
            }

            message.Body = builder.ToMessageBody();
            message.From.Add(new MailboxAddress(_mailSettings.DisplayName, _mailSettings.Email));

            using var smtp = new SmtpClient();
            smtp.Connect(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(_mailSettings.Email, _mailSettings.Password);
            await smtp.SendAsync(message);
            smtp.Disconnect(true);

            _logger.LogWarning($"Sending email to {to} with subject {subject}.");
        }
    }
}
