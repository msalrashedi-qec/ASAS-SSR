using Core.Entities.Workflow;
using Core.Enums;
using Microsoft.JSInterop;
using Shared.Dtos;
using Shared.Dtos.Workflow;
using Web.Extensions;
using static Web.Extensions.Policies;

namespace Web.Components.Pages.WF
{
    public partial class WorkflowProcessesList
    {

        private bool isLoading = false;
        private WorkflowActionDto action = new();

        public IEnumerable<BasicDto> types = new List<BasicDto>();
        public IEnumerable<BasicByteDto> statuses = new List<BasicByteDto>();

        private IEnumerable<InboxDto>? workflows = new List<InboxDto>();
        private int TotalPages { get; set; }
        private int PageSize { get; set; } = 50;
        private int PageIndex { get; set; } = 1;
        private int totalRows;


        IList<int> selectedTypes = new int[] { };
        IList<int> selectedStatuses = new int[] { 2 };


        private WorkflowFilterDto search = new();

        protected async override Task OnInitializedAsync()
        {
            isLoading = true;
            types = _mapper.Map<IEnumerable<BasicDto>>(await _uow.WorkflowTypes.FindAllAsync(x => x.Id > 0));
            statuses = _mapper.Map<IEnumerable<BasicByteDto>>(await _uow.WorkflowStatuses.FindAllAsync(x => x.Id > 1 && x.Id != 12));
            
            await GetListAsync();
            isLoading = false;
        }

        private async Task GetListAsync()
        {
            try
            {
                isLoading = true;

                PagedList<WorkflowFilterDto> filter = new();

                //Type
                if (selectedTypes.Count > 0)
                {
                    search.Types = new List<int>();
                    foreach (byte type in selectedTypes)
                    {
                        search.Types.Add(type);
                    }
                }
                else
                    search.Types = null;

                //Status
                if (selectedStatuses.Count > 0)
                {
                    search.Statuses = new List<byte>();
                    foreach (byte status in selectedStatuses)
                    {
                        search.Statuses.Add(status);
                    }
                }
                else
                    search.Statuses = null;

                filter.Filter = search;
                filter.PageIndex = PageIndex;
                filter.PageSize = PageSize;

                //var user = await _uow.Users.FindAsync(x => x.Id == GetUserId() && x.IsActive && x.Employee.StatusId >= 10 && x.Employee.StatusId <= 30);
                //if (user == null) return Unauthorized("UnuthorizedAccount");

                string userId = await _userService.GetUserIdAsync();
                var userRoles = (await _uow.UserRoles.FindAllAsync(x => x.UserId == userId)).Select(x => x.RoleId).ToList();
                //var permittedOus = (await _uow.RoleOus.FindAllAsync(x => userRoles.Contains(x.RoleId))).Select(x => x.OuId).ToList();

                var list = await _uow.WorkflowProcesses.FindAllAsync(x => x.StatusId > 1 && x.StatusId != 12
                && (filter.Filter.RefNo == null || x.RefNo == filter.Filter.RefNo.Trim())
                && (filter.Filter.Statuses == null || filter.Filter.Statuses.Contains(x.StatusId))
                && (filter.Filter.Types == null || filter.Filter.Types.Contains(x.Workflow.TypeId))
                , new[] { "Status", "WorkflowProcessTasks.WfTask", "RequestedFor", "Workflow.Type" }
                , (filter.PageIndex - 1) * filter.PageSize, filter.PageSize
                , x => x.RefNo, OrderBy.Descending);

                workflows = _mapper.Map<IEnumerable<InboxDto>>(list);
                totalRows = await _uow.WorkflowProcesses.CountAsync(x => x.StatusId > 1 && x.StatusId != 12
                && (filter.Filter.RefNo == null || x.RefNo == filter.Filter.RefNo.Trim())
                && (filter.Filter.Statuses == null || filter.Filter.Statuses.Contains(x.StatusId))
                && (filter.Filter.Types == null || filter.Filter.Types.Contains(x.Workflow.TypeId)));

                TotalPages = totalRows / PageSize;
                if (totalRows % PageSize > 0)
                    TotalPages++;
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

        private void DeleteItem(InboxDto item)
        {
            action.ProcessId = item.WfProcessId;
            action.Comments = string.Empty;
        }
        private async Task CancellProcessAsync()
        {
            if (string.IsNullOrEmpty(action.Comments))
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["EnterReason"]);
                return;
            }
            try
            {
                isLoading = true;
                var process = await _uow.WorkflowProcesses.FindAsync(x => x.Id == action.ProcessId && x.StatusId == 2, new[] { "Workflow", "WorkflowProcessTasks" });
                if (process == null)
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["RequestDetailsNotFound"]);
                    return;
                }
                
                var currentTask= process.WorkflowProcessTasks.FirstOrDefault(x=> x.StatusId == 2);
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
                    _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                }
                else
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["Faild_to_save"]);
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
            }
            finally
            {
                await _js.InvokeVoidAsync("APP.hideModal", "CancelModal");
                isLoading = false;
            }
        }
    }
}