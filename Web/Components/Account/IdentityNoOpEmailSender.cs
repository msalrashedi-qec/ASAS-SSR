

namespace Web.Components.Account
{
    internal sealed class IdentityNoOpEmailSender
    {
        [Inject]
        public IEmailSender EmailSender { get; set; }

        public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
            EmailSender.SendEmailAsync(email, "", "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");
        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
            EmailSender.SendEmailAsync(email, "", "Reset your password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");

        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
            EmailSender.SendEmailAsync(email, "", "Reset your password", $"Please reset your password using the following code: {resetCode}");
    }
}
