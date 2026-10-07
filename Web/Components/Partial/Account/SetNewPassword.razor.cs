using System.ComponentModel.DataAnnotations;
using Web.Components.Account;

namespace Web.Components.Partial.Account
{
    public partial class SetNewPassword
    {
        [Inject]
        public UserManager<ApplicationUser> UserManager { get; set; }

        [Inject]
        private IdentityRedirectManager RedirectManager { get; set; } = default!;

        [CascadingParameter]
        private HttpContext HttpContext { get; set; } = default!;

        [Parameter]
        public string UserId { get; set; } = string.Empty;

        [Parameter]
        public string ResetToken { get; set; } = string.Empty;

        [Parameter]
        public bool Visible { get; set; }

        [Parameter]
        public EventCallback<string> OnError { get; set; }

        private string PanelStyle => Visible ? string.Empty : "display:none;";

        private string ResolvedUserId => string.IsNullOrWhiteSpace(Input.UserId) ? UserId : Input.UserId;

        private string ResolvedResetToken => string.IsNullOrWhiteSpace(Input.ResetToken) ? ResetToken : Input.ResetToken;

        [SupplyParameterFromForm(FormName = "set-new-password")]
        private SetNewPasswordInput Input { get; set; } = default!;

        private bool isLoading;
        private string? errorMessage;

        protected override void OnInitialized()
        {
            Input ??= new();
        }

        private async Task OnSubmitAsync()
        {
            isLoading = true;
            errorMessage = string.Empty;

            try
            {
                var submittedUserId = ResolvedUserId;
                var submittedResetToken = ResolvedResetToken;

                if (string.IsNullOrWhiteSpace(submittedUserId) || string.IsNullOrWhiteSpace(submittedResetToken))
                {
                    errorMessage = _localizer["InvalidVerificationCode"];
                    await OnError.InvokeAsync(submittedUserId);
                    return;
                }

                var user = await UserManager.FindByIdAsync(submittedUserId);
                if (user is null)
                {
                    errorMessage = _localizer["InvalidUser"];
                    await OnError.InvokeAsync(submittedUserId);
                    return;
                }

                var result = await UserManager.ResetPasswordAsync(user, submittedResetToken, Input.NewPassword);
                if (!result.Succeeded)
                {
                    errorMessage = string.Join(", ", result.Errors.Select(error => error.Description));
                    await OnError.InvokeAsync(submittedUserId);
                    return;
                }

                user.OTP = string.Empty;
                user.OTPSentAt = null;
                await UserManager.UpdateAsync(user);

                RedirectManager.RedirectToWithStatus("Account/Login", _localizer["SavedSuccessfully"].Value, HttpContext);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                await OnError.InvokeAsync(ResolvedUserId);
            }
            finally
            {
                isLoading = false;
            }
        }
    }

    public sealed class SetNewPasswordInput
    {
        public string UserId { get; set; } = string.Empty;

        public string ResetToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required.")]
        [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}