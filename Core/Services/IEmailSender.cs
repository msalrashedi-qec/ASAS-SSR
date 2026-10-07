namespace Core.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string to, string cc, string subject, string body, string? attachments = null);
    }
}
