using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Web.Extensions;

namespace Web.Components.Partial.Account
{
    public partial class ResetCodeVerification
    {
        [Inject]
        public IWebHostEnvironment Environment { get; set; }

        [Inject]
        public UserManager<ApplicationUser> UserManager { get; set; }

        [Inject]
        public IEmailSender EmailSender { get; set; }

        [Parameter]
        public string UserId { get; set; } = string.Empty;

        [Parameter]
        public bool Visible { get; set; }

        [Parameter]
        public EventCallback<PasswordResetContext> OnCodeVerified { get; set; }

        [Parameter]
        public EventCallback<string> OnCodeRejected { get; set; }

        private string PanelStyle => Visible ? string.Empty : "display:none;";

        private string ResolvedUserId => string.IsNullOrWhiteSpace(Input.UserId) ? UserId : Input.UserId;

        [SupplyParameterFromForm(FormName = "verify-recovery-code")]
        private InputCodeModel Input { get; set; } = default!;

        private bool isLoading;
        private bool isResending;
        private string? errorMessage;

        protected override void OnInitialized()
        {
            Input ??= new();
        }

        private async Task OnSubmitAsync()
        {
            if (string.Equals(Input.Action, "resend", StringComparison.OrdinalIgnoreCase))
            {
                await ResendCodeAsync();
                return;
            }

            await VerifyCodeAsync();
        }

        private async Task VerifyCodeAsync()
        {
            isLoading = true;
            errorMessage = string.Empty;

            try
            {
                var submittedUserId = ResolvedUserId;
                if (string.IsNullOrWhiteSpace(submittedUserId))
                {
                    errorMessage = _localizer["InvalidVerificationCode"];
                    await OnCodeRejected.InvokeAsync(submittedUserId);
                    return;
                }

                var submittedCode = Input.Code;
                if (submittedCode.Length != 6)
                {
                    errorMessage = _localizer["InvalidVerificationCode"];
                    await OnCodeRejected.InvokeAsync(submittedUserId);
                    return;
                }

                var user = await UserManager.FindByIdAsync(submittedUserId);
                if (user is null)
                {
                    errorMessage = _localizer["InvalidVerificationCode"];
                    await OnCodeRejected.InvokeAsync(submittedUserId);
                    return;
                }

                if (!string.Equals(user.OTP, submittedCode, StringComparison.Ordinal))
                {
                    errorMessage = _localizer["InvalidVerificationCode"];
                    await OnCodeRejected.InvokeAsync(submittedUserId);
                    return;
                }

                var resetToken = await UserManager.GeneratePasswordResetTokenAsync(user);
                await OnCodeVerified.InvokeAsync(new PasswordResetContext(user.Id, resetToken));
            }
            catch (Exception ex)
            {
                _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = _localizer["Error"], Detail = ex.Message });
                await OnCodeRejected.InvokeAsync(ResolvedUserId);
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task ResendCodeAsync()
        {
            isResending = true;
            errorMessage = string.Empty;
            var submittedUserId = ResolvedUserId;

            try
            {
                if (string.IsNullOrWhiteSpace(submittedUserId))
                {
                    errorMessage = _localizer["InvalidVerificationCode"];
                    await OnCodeRejected.InvokeAsync(submittedUserId);
                    return;
                }

                var user = await UserManager.FindByIdAsync(submittedUserId);
                if (user is null)
                {
                    errorMessage = _localizer["InvalidVerificationCode"];
                    await OnCodeRejected.InvokeAsync(submittedUserId);
                    return;
                }

                user.OTP = RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
                user.OTPSentAt = DateTime.UtcNow.GetKsaDateTime();

                var result = await UserManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                    await OnCodeRejected.InvokeAsync(submittedUserId);
                    return;
                }

                var templatePath = Path.Combine(Environment.WebRootPath, "templates", "email", "ForgotPassword.html");
                var body = await File.ReadAllTextAsync(templatePath);
                body = body.Replace("{UserNameAr}", user.Name);
                body = body.Replace("{UserName}", user.Name);
                body = body.Replace("{OTP}", user.OTP);

                await EmailSender.SendEmailAsync(user.Email, "", "Recover Password", body);
                _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = _localizer["Success"], Detail = _localizer["SentSuccessfully"] });

                Input = new InputCodeModel { UserId = submittedUserId };
                await OnCodeRejected.InvokeAsync(submittedUserId);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                await OnCodeRejected.InvokeAsync(submittedUserId);
            }
            finally
            {
                isResending = false;
            }
        }
    }

    public sealed record PasswordResetContext(string UserId, string ResetToken);

    public sealed class InputCodeModel
    {
        public string UserId { get; set; } = string.Empty;

        public string Action { get; set; } = "verify";

        [Required]
        public string Code0 { get; set; } = string.Empty;

        [Required]
        public string Code1 { get; set; } = string.Empty;

        [Required]
        public string Code2 { get; set; } = string.Empty;

        [Required]
        public string Code3 { get; set; } = string.Empty;

        [Required]
        public string Code4 { get; set; } = string.Empty;

        [Required]
        public string Code5 { get; set; } = string.Empty;

        public string Code => string.Concat(Code0, Code1, Code2, Code3, Code4, Code5).Trim();
    }
}