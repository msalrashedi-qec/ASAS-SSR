
using Core.Entities.Workflow;
using Shared.Dtos.Workflow;
using Web.Extensions;

namespace Web.Components.Pages.WF
{
    public partial class ViewProcessDetails
    {
        [Parameter]
        public Guid Id { get; set; }
        

        private WorkflowProcessDto? process;
        private WorkflowActionDto action = new();
        private bool isLoading = false;

        protected override async Task OnParametersSetAsync()
        {
            await GetDetailsAsync();
        }

        private async Task GetDetailsAsync()
        {
            try
            {
                process = null;
                isLoading = true;

                var task = await _uow.WorkflowProcessTasks.FindAsync(x => x.WfProcessId == Id
                    , new[] { "WfProcess.Status", "WfProcess", "WfProcess.Workflow", "WfTask" });
                if (task == null)
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["NotFound"]);
                    return;
                }
                process = _mapper.Map<WorkflowProcessDto>(task);
                process.AllowWithdrawal = task.WfProcess.CreatedBy == await _userService.GetUserIdAsync() && task.WfTask.AllowWithdrawal && task.WfProcess.StatusId == 2;

                var histories = (await _uow.WorkflowHistories.FindAllAsync(x => x.WfProcessId == task.WfProcessId
                , new[] { "Creator", "Action", "Task", "Attachments" })).OrderByDescending(x => x.CreatedOn).ThenByDescending(x => x.ActionId);

                List<WorkflowHistoryDto> mappedHistories = new();
                foreach (var history in histories)
                {
                    var h = new WorkflowHistoryDto();
                    h.ActionTypeId = history.ActionId;
                    h.ActionTypeName = history.Action.Name;
                    h.TaskName = history.Task.Name;
                    h.Comments = history.Comments;
                    h.UserName = history.Creator.Name;
                    h.CreatedOn = history.CreatedOn;
                    var files = history.Attachments.ToList();
                    foreach (var file in files)
                    {
                        h.Attachments.Add(new FileDto { FileName = file.FileName, FilePath = file.FilePath });
                    }
                    mappedHistories.Add(h);
                }
                process.Histories = mappedHistories;
            }
            catch (Exception ex)
            {
                process = null;
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task WithdrawAsync()
        {
            if (process is null)
            {
                return;
            }

            if (string.IsNullOrEmpty(action.Comments))
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["EnterReason"]);
                return;
            }
            action.ProcessId = process.ProcessId;
            try
            {
                isLoading = true;
                var process = await _uow.WorkflowProcesses.FindAsync(x => x.Id == action.ProcessId && x.StatusId == 2, new[] { "Workflow", "WorkflowProcessTasks" });
                if (process == null)
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["RequestDetailsNotFound"]);
                    return;
                }
                var currentTask = process.WorkflowProcessTasks.FirstOrDefault(x => x.StatusId == 2);
                if (currentTask is null)
                    currentTask = process.WorkflowProcessTasks.OrderByDescending(x => x.StartDate).FirstOrDefault();

                await _uow.WorkflowHistories.AddAsync(new WorkflowHistory
                {
                    WfProcessId = process.Id,
                    ActionId = 5,
                    TaskId = currentTask != null ? currentTask.WfTaskId : 0,
                    Comments = action.Comments,
                    CreatorId = await _userService.GetUserIdAsync(),
                    CreatedOn = DateTime.UtcNow.GetKsaDateTime()
                });

                var nextTasks = await _uow.WorkflowProcessTasks.FindAllAsync(x => x.WfProcessId == process.Id && x.StatusId == 2);
                if (nextTasks.Any())
                {
                    foreach (var task in nextTasks)
                    {
                        task.StatusId = 20;
                        task.StartDate = DateTime.UtcNow.GetKsaDateTime();
                        task.EndDate = DateTime.UtcNow.GetKsaDateTime();
                        await _uow.WorkflowProcessTasks.Update(task);
                    }
                }

                process.StatusId = 20;
                await _uow.WorkflowProcesses.Update(process);
                if (await _uow.SaveAsync())
                {
                    //await _hub.Clients.All.SendAsync("wf", "Workflow process cancelled");
                    //return Ok(201);
                    _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                    _navigator.NavigateTo("my-requests");
                }
                else
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["Faild_to_save"]);
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer[ex.Message]);
            }
            finally
            {
                isLoading = false;
            }
            
        }
    }
}
