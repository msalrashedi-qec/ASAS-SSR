
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Web.Extensions;


namespace Web.Components.Partial.Account
{
    public partial class SendRecoveryCode
    {
        [Inject]
        public IWebHostEnvironment Environment { get; set; }

        [Inject]
        public UserManager<ApplicationUser> UserManager { get; set; }

        [Parameter]
        public bool Visible { get; set; } = true;

        [Parameter]
        public EventCallback<string> OnCodeSent { get; set; }

        [Inject]
        public IEmailSender EmailSender { get; set; }

        private string PanelStyle => Visible ? string.Empty : "display:none;";

        [SupplyParameterFromForm(FormName = "forgot-password")]
        private InputModel Input { get; set; } = default!;
        private bool isLoading;
        private string? errorMessage;

        protected override void OnInitialized()
        {
            Input ??= new();
        }

        private async Task OnValidSubmitAsync()
        {
            try
            {
                isLoading = true;
                errorMessage = string.Empty;
                var user = await UserManager.FindByEmailAsync(Input.Email);
                if (user is null)
                {
                    errorMessage = _localizer["UserNotFoundWithEmail"];
                    return;
                }

                user.OTP = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
                user.OTPSentAt = DateTime.UtcNow.GetKsaDateTime();

                var result = await UserManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    string body = string.Empty;
                    using (var reader = new StreamReader($"{Environment.WebRootPath}/templates/email/ForgotPassword.html"))
                    {
                        body = reader.ReadToEnd();
                    }
                    body = body.Replace("{UserNameAr}", user.Name);
                    body = body.Replace("{UserName}", user.Name);
                    body = body.Replace("{OTP}", user.OTP);

                    await EmailSender.SendEmailAsync(user.Email, "", "Recover Password", body);
                    await OnCodeSent.InvokeAsync(user.Id);
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    errorMessage = errors;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            finally
            {
                isLoading = false;
            }
        }
    }


    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}
