using Microsoft.JSInterop;
using Shared.Dtos.Account;
using System.Security.Claims;
using Web.Extensions;

namespace Web.Components.Pages.Account
{
    public partial class RoleList
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuthorizationUpdateNotifier _authorizationUpdateNotifier;

        public RoleList(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, AuthorizationUpdateNotifier authorizationUpdateNotifier)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _authorizationUpdateNotifier = authorizationUpdateNotifier;
        }

        private RoleDto _model = new();
        private IEnumerable<RoleDto> roles = new List<RoleDto>();
        private List<ReportDto> reportPermissions = new();

        private bool isLoading;
        private bool isProcessing;
        private string searchTerm = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            await GetListAsync();
        }

        private async Task GetListAsync()
        {
            isLoading = true;
            try
            {
                var allRoles = _roleManager.Roles.ToList();
                var roleDtos = new List<RoleDto>();

                foreach (var role in allRoles)
                {
                    var dto = new RoleDto()
                    {
                        Id = role.Id,
                        Name = role.Name,
                        NormalizedName = role.NormalizedName,
                        ConcurrencyStamp = role.ConcurrencyStamp,
                        Description = role.NormalizedName == "ADMIN" ? _localizer["AdministratorRole"] : role.Name
                    };

                    // Get user count for this role
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
                    dto.UserCount = usersInRole.Count;

                    // Get role claims
                    var claims = await _roleManager.GetClaimsAsync(role);
                    dto.Claims = claims.Select(c => c.Value).ToList();

                    roleDtos.Add(dto);
                }

                roles = roleDtos.OrderByDescending(r => r.UserCount);
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
            }
            finally
            {
                isLoading = false;
            }
        }

        private IEnumerable<RoleDto> GetFilteredRoles()
        {
            var filtered = roles;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filtered = filtered.Where(r =>
                    (r.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                );
            }

            return filtered.ToList();
        }

        private void New()
        {
            _model = new RoleDto();
        }

        private void Edit(RoleDto item)
        {
            _model = new RoleDto()
            {
                Id = item.Id,
                Name = item.Name,
                NormalizedName = item.NormalizedName,
                Description = item.Description,
                UserCount = item.UserCount,
                Claims = item.Claims,
                Permissions = item.Permissions
            };
        }

        private async Task SaveAsync()
        {
            isProcessing = true;
            try
            {
                // Validate role name
                if (string.IsNullOrWhiteSpace(_model.Name))
                {

                    _toastService.Notify(NotificationSeverity.Warning, _localizer["Validation"], _localizer["RoleNameIsRequired"]);
                    await _js.InvokeVoidAsync("APP.hideModal", "RoleDetailsModal");
                    return;
                }

                // Check if role name already exists (for new roles)
                if (string.IsNullOrWhiteSpace(_model.Id))
                {
                    var existingRole = await _roleManager.FindByNameAsync(_model.Name);
                    if (existingRole != null)
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }
                    // Create new role
                    var newRole = new ApplicationRole
                    { 
                        Name = _model.Name.Trim()
                    };

                    var result = await _roleManager.CreateAsync(newRole);
                    if (result.Succeeded)
                    {
                        _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                        _model = new RoleDto();
                        await GetListAsync();
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], errors);
                    }
                }
                else
                {
                    // Update existing role
                    var role = await _roleManager.FindByIdAsync(_model.Id);
                    if (role == null)
                    {
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["RoleNotFound"]);
                        return;
                    }

                    // Update description if provided (role name is immutable)
                    if (!string.IsNullOrWhiteSpace(_model.Description))
                    {
                        role.NormalizedName = _model.Description;
                    }

                    var result = await _roleManager.UpdateAsync(role);
                    if (result.Succeeded)
                    {
                        _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                        _model = new RoleDto();
                        await GetListAsync();
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Error"],errors);
                    }
                }
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
            }
            finally
            {
                await _js.InvokeVoidAsync("APP.hideModal", "RoleDetailsModal");
                isProcessing = false;
            }
        }

        private async Task DeleteAsync(RoleDto item)
        {
            if (item.UserCount > 0)
            {
                _toastService.Notify(NotificationSeverity.Warning, _localizer["Warning"], _localizer["CannotDeleteRoleWithUsers", item.UserCount]);
                await _js.InvokeVoidAsync("APP.hideModal", "DeleteModal");
                return;
            }

            isProcessing = true;
            try
            {
                var role = await _roleManager.FindByIdAsync(item.Id);
                if (role != null)
                {
                    var result = await _roleManager.DeleteAsync(role);
                    if (result.Succeeded)
                    {
                        _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                        await GetListAsync();
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], errors);
                    }
                }
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.ToString());
            }
            finally
            {
                await _js.InvokeVoidAsync("APP.hideModal", "DeleteModal");
                isProcessing = false;
            }
        }

        private async Task SaveReportPermissionsAsync()
        {
            isProcessing = true;

            try
            {
                if (string.IsNullOrWhiteSpace(_model.Id))
                    return;

                var existingRoleReports = (await _uow.RoleReports
                    .FindAllAsync(x => x.RoleId == _model.Id))
                    .ToList();
                var existingReportIds = existingRoleReports
                    .Select(x => x.ReportId)
                    .ToHashSet();
                var selectedReportIds = reportPermissions
                    .Where(x => x.IsPermitted)
                    .Select(x => x.Id)
                    .ToHashSet();

                var toRemove = existingRoleReports
                    .Where(x => !selectedReportIds.Contains(x.ReportId))
                    .ToList();
                var toAdd = selectedReportIds.Except(existingReportIds).ToList();

                if (toRemove.Count == 0 && toAdd.Count == 0)
                {
                    _toastService.Notify(NotificationSeverity.Info, _localizer["Save"], _localizer["NoChangesFound"]);
                    return;
                }

                if (toRemove.Count > 0)
                    _uow.RoleReports.DeleteRange(toRemove);

                if (toAdd.Count > 0)
                {
                    var userId = await _userService.GetUserIdAsync();
                    var now = DateTime.UtcNow.GetKsaDateTime();
                    await _uow.RoleReports.AddRangeAsync(toAdd.Select(reportId => new RoleReport
                    {
                        Id = Guid.NewGuid(),
                        RoleId = _model.Id,
                        ReportId = reportId,
                        CreatedBy = userId,
                        CreatedOn = now
                    }));
                }

                if (await _uow.SaveAsync())
                    _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                else
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
            }
            catch (Exception ex)
            {
                _toastService.Notify(
                    NotificationSeverity.Error,
                    "Error",
                    ex.ToString());
            }
            finally
            {
                isProcessing = false;
                await _js.InvokeVoidAsync("APP.hideModal", "ReportPermissionsModal");
            }
        }

        private async Task SavePermissionsAsync()
        {
            isProcessing = true;

            try
            {
                var role = await _roleManager.FindByIdAsync(_model.Id);

                if (role == null)
                    return;

                var existingClaims = await _roleManager.GetClaimsAsync(role);
                var existingPermissions = existingClaims
                    .Where(x => x.Type == "Permission")
                    .Select(x => x.Value)
                    .ToHashSet();

                var selectedPermissions = _model.Permissions
                    .Where(x => x.IsGranted)
                    .Select(x => x.Name)
                    .ToHashSet();

                // Only remove permissions that were unchecked
                var toRemove = existingPermissions.Except(selectedPermissions);
                foreach (var permission in toRemove)
                {
                    await _roleManager.RemoveClaimAsync(role, new Claim("Permission", permission));
                }

                // Only add permissions that are newly checked
                var toAdd = selectedPermissions.Except(existingPermissions);
                foreach (var permission in toAdd)
                {
                    await _roleManager.AddClaimAsync(role, new Claim("Permission", permission));
                }


                // Refresh only if there were actual changes
                if (toRemove.Any() || toAdd.Any())
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
                    await _authorizationUpdateNotifier.PublishAsync(usersInRole.Select(user => user.Id));
                }

                _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);

                await GetListAsync();
            }
            catch (Exception ex)
            {
                _toastService.Notify(
                    NotificationSeverity.Error,
                    "Error",
                    ex.ToString());
            }
            finally
            {
                isProcessing = false;
                await _js.InvokeVoidAsync("APP.hideModal", "PermissionsModal");
            }
        }

        private string GetUserCountBadge(int count)
        {
            if (count == 0) return "badge bg-secondary";
            if (count < 5) return "badge bg-info";
            if (count < 10) return "badge bg-warning";
            return "badge bg-danger";
        }

        private async Task GetPermissions(RoleDto item)
        {
            _model = item;

            var role = await _roleManager.FindByIdAsync(item.Id);
            if (role == null)
                return;
            // Existing role claims
            var roleClaims = await _roleManager.GetClaimsAsync(role);

            // Convert to permission list
            _model.Permissions = Policies.AllPermissions
                .Select(permission => new PermissionDto
                {
                    Name = permission,
                    Description = permission,
                    IsGranted = roleClaims.Any(c =>
                        c.Type == "Permission" &&
                        c.Value == permission)
                })
                .ToList();

        }
        private async Task GetReportPermissionsAsync(RoleDto item)
        {
            _model = item;
            reportPermissions = new();
            isProcessing = true;

            try
            {
                var reports = await _uow.Reports.GetAllAsync();
                var permittedReportIds = (await _uow.RoleReports
                    .FindAllAsync(x => x.RoleId == item.Id))
                    .Select(x => x.ReportId)
                    .ToHashSet();

                reportPermissions = reports
                    .OrderBy(x => x.SN)
                    .Select(report => new ReportDto
                    {
                        Id = report.Id,
                        Name = report.Name,
                        NameAr = report.NameAr,
                        Icon = report.Icon,
                        Url = report.Url,
                        SN = report.SN,
                        IsPermitted = permittedReportIds.Contains(report.Id)
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
            }
            finally
            {
                isProcessing = false;
            }
        }

        private static string GetReportName(ReportDto report)
        {
            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
            return isArabic && !string.IsNullOrWhiteSpace(report.NameAr)
                ? report.NameAr
                : report.Name ?? report.NameAr ?? string.Empty;
        }

        private async Task GetUsersInRoleAsync(string? roleName)
        {
            _model.UsersInRole.Clear();
            _model.Name = roleName ?? "";

            if (string.IsNullOrWhiteSpace(roleName))
                return;

            var users = await _userManager.GetUsersInRoleAsync(roleName);

            if (users.Count == 0)
                return;

            var userIds = users.Select(user => user.Id).ToHashSet();

            var userSchools = await _uow.UserSchools.FindAllAsync(x => userIds.Contains(x.UserId), new[] { "School" });

            var schoolsByUser = userSchools
                .GroupBy(userSchool => userSchool.UserId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(userSchool => new BasicGuidDto
                    {
                        Id = userSchool.SchoolId,
                        Name = userSchool.School.Name
                    }).ToList());

            _model.UsersInRole.AddRange(
                users.Select(user => new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Projects = schoolsByUser.GetValueOrDefault(user.Id) ?? []
                }));
        }
    }
}
