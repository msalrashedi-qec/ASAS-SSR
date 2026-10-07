using Microsoft.EntityFrameworkCore;
using Shared.Dtos.Account;
using System.ComponentModel.DataAnnotations;

namespace Web.Components.Pages.Account
{
    public partial class UserList
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly AuthorizationUpdateNotifier _authorizationUpdateNotifier;
        private readonly WorkflowUpdateNotifier _workflowUpdateNotifier;

        public UserList(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, AuthorizationUpdateNotifier authorizationUpdateNotifier, WorkflowUpdateNotifier workflowUpdateNotifier)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _authorizationUpdateNotifier = authorizationUpdateNotifier;
            _workflowUpdateNotifier = workflowUpdateNotifier;
        }

        private ApplicationUserDto _model = new();
        private IEnumerable<ApplicationUserDto> users = new List<ApplicationUserDto>();
        private IEnumerable<ApplicationRole> roles = new List<ApplicationRole>();
        private IEnumerable<BasicGuidDto> schools = new List<BasicGuidDto>();


        private bool isLoading;
        private bool isProcessing;
        private bool isChangingPassword;
        private string searchTerm = string.Empty;
        private PasswordChangeModel _passwordModel = new();

        protected override async Task OnInitializedAsync()
        {
            await GetListAsync();
            await GetRolesAsync();
            schools = _mapper.Map<IEnumerable<BasicGuidDto>>(await _uow.Schools.GetAllAsync());
        }

        private async Task GetListAsync()
        {
            isLoading = true;
            try
            {
                var allUsers = _userManager.Users.Where(x=> x.Id != Guid.Empty.ToString()).OrderByDescending(x=> x.IsActive).ThenBy(x=> x.Name).ToList();
                var userDtos = new List<ApplicationUserDto>();

                foreach (var user in allUsers)
                {
                    var dto = _mapper.Map<ApplicationUserDto>(user);
                    
                    // Get the names first
                    var roleNames = await _userManager.GetRolesAsync(user);

                    // Find the IDs based on those names
                    foreach (var name in roleNames)
                    {
                        var role = await _roleManager.FindByNameAsync(name);
                        if (role != null)
                            dto.GrantedRoles.Add(role.Id);
                    }
                    dto.GrantedSchools = (await _uow.UserSchools.FindAllAsync(x => x.UserId == user.Id)).Select(x => x.SchoolId).ToList();
                    userDtos.Add(dto);
                }

                users = userDtos;
            }
            catch (Exception ex)
            {
                _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Error", Detail = $"Failed to load users: {ex.Message}" });
            }
            finally
            {
                isLoading = false;
            }
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
        private IEnumerable<ApplicationUserDto> GetFilteredUsers()
        {
            var filtered = users;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filtered = filtered.Where(u =>
                    (u.UserName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.Email?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                );
            }

            return filtered.ToList();
        }
        private void Edit(ApplicationUserDto item)
        {
            _model = new ApplicationUserDto()
            {
                Id = item.Id,
                Name = item.Name,
                UserName = item.UserName,
                Email = item.Email,
                DefaultLanguage = item.DefaultLanguage,
                AuthFactorStartDate = item.AuthFactorStartDate,
                SuspenseDate = item.SuspenseDate,
                IsActive = item.IsActive,
                PhoneNumber = item.PhoneNumber,
                EmailConfirmed = item.EmailConfirmed,
                PhoneNumberConfirmed = item.PhoneNumberConfirmed,
                TwoFactorEnabled = item.TwoFactorEnabled,
                CreatedOn = item.CreatedOn,
                GrantedRoles = item.GrantedRoles,
                GrantedSchools = item.GrantedSchools
            };
        }

        private void OpenPasswordModal(ApplicationUserDto user)
        {
            _passwordModel = new PasswordChangeModel
            {
                UserId = user.Id,
                UserName = user.Name ?? string.Empty
            };
        }

        private async Task ChangePasswordAsync()
        {
            isChangingPassword = true;
            try
            {
                var user = await _userManager.FindByIdAsync(_passwordModel.UserId);
                if (user == null)
                {
                    _toastService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = "User not found"
                    });
                    return;
                }

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, resetToken, _passwordModel.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(error => error.Description));
                    _toastService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Error",
                        Detail = errors
                    });
                    return;
                }

                _toastService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Success",
                    Detail = "Password changed successfully"
                });
                _passwordModel = new PasswordChangeModel();
                await _js.InvokeVoidAsync("APP.hideModal", "ChangePasswordModal");
            }
            catch (Exception ex)
            {
                _toastService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = ex.Message
                });
            }
            finally
            {
                isChangingPassword = false;
            }
        }

        private async Task SaveAsync()
        {
            isProcessing = true;
            try
            {
                var user = await _userManager.FindByIdAsync(_model.Id);
                if (user == null)
                {
                    _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Error", Detail = "User not found" });
                    await _js.InvokeVoidAsync("APP.hideModal", "DetailsModal");
                    return;
                }

                _mapper.Map(_model, user);
                user.UpdatedBy = await _userService.GetUserIdAsync();
                user.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    var desiredRoles = roles
                        .Where(role => _model.GrantedRoles.Contains(role.Id))
                        .Select(role => role.Name!)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var rolesToRemove = userRoles.Except(desiredRoles, StringComparer.OrdinalIgnoreCase).ToArray();
                    var rolesToAdd = desiredRoles.Except(userRoles, StringComparer.OrdinalIgnoreCase).ToArray();

                    var removeResult = rolesToRemove.Length == 0
                        ? IdentityResult.Success
                        : await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                    var addResult = rolesToAdd.Length == 0
                        ? IdentityResult.Success
                        : await _userManager.AddToRolesAsync(user, rolesToAdd);

                    if (!removeResult.Succeeded || !addResult.Succeeded)
                    {
                        var roleErrors = removeResult.Errors
                            .Concat(addResult.Errors)
                            .Select(error => error.Description);
                        throw new InvalidOperationException(string.Join(", ", roleErrors));
                    }

                    if (rolesToRemove.Length > 0 || rolesToAdd.Length > 0)
                    {
                        await _authorizationUpdateNotifier.PublishAsync([user.Id]);
                        _workflowUpdateNotifier.Publish(WorkflowUpdateKind.Changed);
                    }
                    var userSchools = await _uow.UserSchools.FindAllAsync(x => x.UserId == user.Id);
                    _uow.UserSchools.DeleteRange(userSchools);

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

                    _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Success", Detail = "User updated successfully" });
                    _model = new ApplicationUserDto();
                    await GetListAsync();
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Error", Detail = $"Failed to update user: {errors}" });
                }
            }
            catch (Exception ex)
            {
                _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Error", Detail = ex.Message });
            }
            finally
            {
                await _js.InvokeVoidAsync("APP.hideModal", "DetailsModal");
                isProcessing = false;
            }
        }
        private string GetStatusBadge(bool isActive)
        {
            return isActive ? "badge bg-success" : "badge bg-danger";
        }
        private string GetStatusText(bool isActive)
        {
            return isActive ? _localizer["Active"] :_localizer["Inactive"];
        }

        private sealed class PasswordChangeModel
        {
            public string UserId { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;

            [Required(ErrorMessage = "New password is required.")]
            [StringLength(50, MinimumLength = 1, ErrorMessage = "The password must be between 1 and 50 characters.")]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password confirmation is required.")]
            [DataType(DataType.Password)]
            [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}
