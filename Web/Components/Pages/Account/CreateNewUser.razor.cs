using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos.Account;
using Web.Extensions;

namespace Web.Components.Pages.Account
{
    public partial class CreateNewUser
    {
        private readonly RoleManager<ApplicationRole> _roleManager;

        public CreateNewUser(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
        }


        private ApplicationUserDto _model { get; set; } = new();
        private IEnumerable<ApplicationRole> roles = new List<ApplicationRole>();
        private IEnumerable<BasicGuidDto> schools = new List<BasicGuidDto>();


        private bool isLoading;


        protected override async Task OnInitializedAsync()
        {
            await GetRolesAsync();
            schools = _mapper.Map<IEnumerable<BasicGuidDto>>(await _uow.Schools.GetAllAsync());
        }

        private async Task GetRolesAsync()
        {
            try
            {
                roles = await _roleManager.Roles.ToListAsync();
            }
            catch (Exception ex)
            {
                _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Error", Detail = $"Failed to load roles: {ex.Message}" });
            }
        }

        public async Task RegisterUser(EditContext editContext)
        {
            if (await _uow.Users.Existing(x => x.UserName.ToLower() == _model.UserName.ToLower().Trim()))
            {
                _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Error", Detail = "اسم المستخدم محجوز لحساب آخر" });
                return;
            }


            if (await _uow.Users.Existing(x => x.Email.ToLower() == _model.Email.ToLower().Trim()))
            {
                _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Error", Detail = "البريد الالكتروني مستخدم لحساب آخر" });
                return;
            }
            isLoading = true;
            var user = CreateUser();
            user.Name = _model.Name;
            user.PhoneNumber = _model.PhoneNumber;
            user.CreatedBy = await _userService.GetUserIdAsync();
            user.CreatedOn = DateTime.UtcNow.GetKsaDateTime();

            await UserStore.SetUserNameAsync(user, _model.UserName.ToLower(), CancellationToken.None);
            var emailStore = GetEmailStore();
            await emailStore.SetEmailAsync(user, _model.Email.ToLower(), CancellationToken.None);
            var result = await UserManager.CreateAsync(user, _model.Password);

            isLoading = false;
            if (!result.Succeeded)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
                return;
            }

            foreach (var roleId in _model.GrantedRoles)
            {
                var role = roles.FirstOrDefault(r => r.Id == roleId);
                if (role != null)
                    await UserManager.AddToRoleAsync(user, role.Name);
            }

            foreach (var school in _model.GrantedSchools)
            {
                await _uow.UserSchools.AddAsync(new UserSchool
                {
                    UserId = user.Id,
                    SchoolId = school,
                    CreatedBy = await _userService.GetUserIdAsync(),
                    CreatedOn = DateTime.UtcNow.GetKsaDateTime(),
                });
            }

            await _uow.SaveAsync();

            Logger.LogInformation("User created a new account with password.");
            _navigator.NavigateTo("system/users");
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor.");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!UserManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)UserStore;
        }

    }
}
