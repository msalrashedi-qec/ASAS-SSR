
using Core.Entities.Workflow;
using Shared.Dtos;
using Shared.Dtos.Workflow;
using Web.Extensions;

namespace Web.Components.Pages.WF
{
    public partial class ShareTask
    {
        [Inject]
        public UserManager<ApplicationUser> UserManager { get; set; }

        private bool isLoading = false;

        public IEnumerable<BasicStringDto>? users = new List<BasicStringDto>();
        public IEnumerable<BasicDto>? types = new List<BasicDto>();

        private IEnumerable<InboxDto>? tasks = new List<InboxDto>();
        private int TotalPages { get; set; }
        private int PageSize { get; set; } = 50;
        private int PageIndex { get; set; } = 1;
        private int totalRows;
        private ShareTasksDto dto = new();

        IList<int> selectedTypes = new int[] { };
        IList<int> selectedStatuses = new int[] { 2 };


        private WorkflowFilterDto search = new();

        protected async override Task OnInitializedAsync()
        {
            isLoading = true;
            types = _mapper.Map<IEnumerable<BasicDto>>(await _uow.WorkflowTypes.FindAllAsync(x => x.Id > 0));
            users = UserManager.Users.ToList().Select(x => new BasicStringDto { Id = x.Id, Name = x.Name });
            await GetListAsync();
            isLoading = false;
        }

        private async Task GetListAsync()
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

            //var userRoles = (await _uow.UserRoles.FindAllAsync(x => x.UserId == GetUserId())).Select(x => x.RoleId).ToList();
            //var permittedOus = (await _uow.RoleOus.FindAllAsync(x => userRoles.Contains(x.RoleId))).Select(x => x.OuId).ToList();

            var list = await _uow.WorkflowProcessTasks.FindAllAsync(x => !x.WfTask.IsDeleted
            && x.StatusId > 1 && x.StatusId != 12
            && (filter.Filter.RefNo == null || filter.Filter.RefNo.Contains(x.WfProcess.RefNo))
            && (filter.Filter.Statuses == null || filter.Filter.Statuses.Contains(x.StatusId))
            && (filter.Filter.Types == null || filter.Filter.Types.Contains(x.WfProcess.Workflow.TypeId))
            , new[] { "Status", "WfTask", "WfProcess", "WfProcess.RequestedFor", "WfProcess.Workflow.Type" }
            , (filter.PageIndex - 1) * filter.PageSize, filter.PageSize
            , x => x.WfProcess.RefNo, "DESC");

            tasks = _mapper.Map<IEnumerable<InboxDto>>(list);
            totalRows = await _uow.WorkflowProcessTasks.CountAsync(x => !x.WfTask.IsDeleted
            && x.StatusId > 1 && x.StatusId != 12
            && (filter.Filter.RefNo == null || filter.Filter.RefNo.Contains(x.WfProcess.RefNo))
            && (filter.Filter.Statuses == null || filter.Filter.Statuses.Contains(x.StatusId))
            && (filter.Filter.Types == null || filter.Filter.Types.Contains(x.WfProcess.Workflow.TypeId))
            );

            TotalPages = totalRows / PageSize;
            if (totalRows % PageSize > 0)
                TotalPages++;

            isLoading = false;
        }

        private void UpdateSelection(ChangeEventArgs arg)
        {
            foreach (var item in tasks)
            {
                item.IsSelected = Convert.ToBoolean(arg.Value);
            }
        }
        private async Task ShareAsync()
        {
            try
            {
                var list = tasks.Where(x => x.IsSelected);
                if (!list.Any())
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["SelectTasksToShare"]);
                    return;
                }

                isLoading = true;

                foreach (var item in list)
                {
                    if (item.IsSelected)
                        dto.WfTasks.Add(item.Id);
                }

                if (!dto.WfTasks.Any())
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["SelectTasksToShare"]);
                    return;
                }

                List<WorkflowProcessTask> sharedTasks = new List<WorkflowProcessTask>();
                List<WorkflowHistory> histories = new List<WorkflowHistory>();

                foreach (var taskId in dto.WfTasks)
                {
                    var wfTask = await _uow.WorkflowProcessTasks.FindAsync(x => x.Id == taskId);
                    if (wfTask == null)
                    {
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["RequestDetailsNotFound"]);
                        isLoading = false;
                        return;
                    }
                    wfTask.SharedWithUserId = dto.SharedWithUserId;
                    wfTask.ShareStartDate = dto.StartDate;
                    wfTask.ShareEndDate = dto.EndDate;
                    sharedTasks.Add(wfTask);

                    histories.Add(new WorkflowHistory
                    {
                        WfProcessId = wfTask.WfProcessId,
                        ActionId = 10,
                        TaskId = wfTask.WfTaskId,
                        Comments = dto.Comments,
                        CreatorId = await _userService.GetUserIdAsync(),
                        CreatedOn = DateTime.UtcNow.GetKsaDateTime()
                    });
                }

                await _uow.WorkflowProcessTasks.UpdateRange(sharedTasks);
                await _uow.WorkflowHistories.AddRangeAsync(histories);
                if (await _uow.SaveAsync())
                {
                    //await _hub.Clients.All.SendAsync("wf", "New tasks shared");
                    _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
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