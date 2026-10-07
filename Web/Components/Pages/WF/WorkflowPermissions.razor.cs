
using Core.Entities.Workflow;
using Shared.Dtos.Account;
using Shared.Dtos.Workflow;
using Web.Extensions;

namespace Web.Components.Pages.WF
{
    public partial class WorkflowPermissions
    {
        [Parameter]
        public int Id { get; set; }

        [Parameter]
        public int WorkflowId { get; set; }


        private string pagePath = "";
        private WfPermissionsDto wfPermissions = new();

        private bool isLoading = false;

        protected async override Task OnInitializedAsync()
        {
            try
            {
                isLoading = true;
                pagePath = $"wftasks/{WorkflowId}";

                var task = await _uow.WorkflowTasks.FindAsync(x => x.Id == Id, new[] { "Workflow" });
                if (task == null)
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["NotFound"]);
                    return;
                }
                wfPermissions = new WfPermissionsDto
                {
                    WfTask = _mapper.Map<BasicDto>(task),
                    Permissions = _mapper.Map<List<PermissionDto>>(await _uow.Permissions.GetAllAsync())
                };

                foreach (var permission in wfPermissions.Permissions)
                {
                    permission.IsGranted = await _uow.WorkflowPermissions.Existing(x => x.WorkflowTaskId == Id && x.PermissionId == permission.Id);
                }
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

        private async Task SaveDataAsync()
        {
            isLoading = true;

            if (wfPermissions.WfTask != null)//In case of Edit
            {
                bool hasChanges = false;
                foreach (var permission in wfPermissions.Permissions)
                {
                    if (permission.IsGranted && !await _uow.WorkflowPermissions.Existing(x => x.WorkflowTaskId == wfPermissions.WfTask.Id && x.PermissionId == permission.Id))
                    {
                        hasChanges = true;
                        await _uow.WorkflowPermissions.AddAsync(new WorkflowPermission
                        {
                            PermissionId = permission.Id,
                            WorkflowTaskId = wfPermissions.WfTask.Id,
                            CreatedBy = await _userService.GetUserIdAsync(),
                            CreatedOn = DateTime.UtcNow.GetKsaDateTime()
                        });
                    }
                    else if (!permission.IsGranted)
                    {
                        var permits = await _uow.WorkflowPermissions.FindAllAsync(x => x.WorkflowTaskId == wfPermissions.WfTask.Id && x.PermissionId == permission.Id);
                        if (permits != null)
                        {
                            _uow.WorkflowPermissions.DeleteRange(permits);
                            hasChanges = true;
                        }
                    }
                }
                if (hasChanges)
                {
                    if (await _uow.SaveAsync())
                    {
                        _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                    }
                    else
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
                }
                else
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["Save"], _localizer["NoChangesFound"]);
            }

            isLoading = false;
        }

    }
}
