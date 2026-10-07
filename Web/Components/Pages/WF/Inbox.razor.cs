using Shared.Dtos.Workflow;

namespace Web.Components.Pages.WF
{
    public partial class Inbox : IDisposable
    {
        [Inject] private WorkflowUpdateNotifier WorkflowUpdateNotifier { get; set; } = default!;

        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private bool isLoading = false;
        private bool _disposed;
        private byte sortColumn = 0;

        public IEnumerable<BasicDto>? types = new List<BasicDto>();
        public IEnumerable<BasicByteDto>? statuses = new List<BasicByteDto>();
        private IEnumerable<BasicGuidDto> organizations = new List<BasicGuidDto>();

        private IEnumerable<InboxDto>? workflows;
        private int TotalPages { get; set; }
        private int PageSize { get; set; } = 50;
        private int PageIndex { get; set; } = 1;
        private int totalRows;

        private WorkflowFilterDto search = new();
        private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

        protected async override Task OnInitializedAsync()
        {
            WorkflowUpdateNotifier.Updated += OnWorkflowUpdatedAsync;
            isLoading = true;
            types = _mapper.Map<IEnumerable<BasicDto>>(await _uow.WorkflowTypes.FindAllAsync(x => x.Id > 0));
            statuses = _mapper.Map<IEnumerable<BasicByteDto>>(await _uow.WorkflowStatuses.GetAllAsync());
            organizations = _mapper.Map<IEnumerable<BasicGuidDto>>(await _uow.Schools.GetAllAsync());
            
            await GetListAsync();

            isLoading = false;

        }

        private Task OnWorkflowUpdatedAsync(WorkflowUpdate update)
        {
            if (_disposed)
                return Task.CompletedTask;

            return InvokeAsync(async () =>
            {
                var previousProcessIds = workflows?.Select(x => x.WfProcessId).ToHashSet() ?? [];
                // A role change can invalidate the current page entirely. Start at
                // the first page while retaining the user's search filters.
                PageIndex = 1;
                await GetListAsync();

                if (update.Kind == WorkflowUpdateKind.ProcessCreated &&
                    workflows?.Any(x => !previousProcessIds.Contains(x.WfProcessId)) == true)
                {
                    _toastService.Notify(NotificationSeverity.Info, IsArabic ? "طلب جديد" : "New request", IsArabic ? "تم إسناد طلب جديد إليك." : "A new request has been assigned to you.");
                }

                StateHasChanged();
            });
        }

        private async Task Search()
        {
            await GetListAsync();
        }

        private async Task SortData(byte columnId)
        {
            search.Language = _localizer["SelectedLanguage"];
            search.SortColumn = columnId;
            if (columnId == sortColumn && search.OrderBy == "ASC")
                search.OrderBy = "DESC";
            else if (columnId == sortColumn && search.OrderBy == "DESC")
                search.OrderBy = "ASC";
            else
                search.OrderBy = "ASC";

            sortColumn = columnId;
            await GetListAsync();
        }
        private async Task GetListAsync()
        {
            await _refreshLock.WaitAsync();
            try
            {
                if (_disposed)
                    return;

                isLoading = true;
                string userId = await _userService.GetUserIdAsync();
                var userRoles = await _uow.UserRoles.FindAllAsync(x => x.UserId == userId);
                var rolesList = userRoles.Select(x=> x.RoleId).ToList();

                var list = await _uow.WorkflowProcessTasks.FindAllAsync(x =>
                rolesList.Contains(x.WfTask.ResponsibleRoleId)
                && (search.RefNo == null || search.RefNo.Contains(x.WfProcess.RefNo))
                && x.StatusId > 1 && x.StatusId != 12 
                && (!search.Statuses.Any() || search.Statuses.Contains(x.StatusId))
                && (search.Types == null || search.Types.Contains(x.WfProcess.Workflow.TypeId))
                && (search.Projects == null || search.Projects.Contains(x.WfProcess.RequestedFor.SchoolId))
                
                , new[] { "Status", "WfTask", "WfProcess", "WfProcess.Workflow.Type", "WfProcess.RequestedFor", "WfProcess.RequestedFor.School" }
                , (PageIndex - 1) * PageSize, PageSize
                ,
                 x => search.SortColumn == 1 ? x.WfProcessId
                 : search.SortColumn == 2 ? x.WfProcess.Workflow.Type.Name
                 : search.SortColumn == 3 ? x.WfProcess.RequestedFor.School.Name
                 : search.SortColumn == 4 ? x.WfTask.Name
                 : search.SortColumn == 5 ? x.StartDate
                 : search.SortColumn == 6 ? x.Status.Name
                 : search.SortColumn == 7 ? x.WfProcess.RequestedFor.Name
                 : x.StartDate
                , search.OrderBy);

                workflows = _mapper.Map<IEnumerable<InboxDto>>(list);

                totalRows = await _uow.WorkflowProcessTasks.CountAsync(x =>
                rolesList.Contains(x.WfTask.ResponsibleRoleId)
                && (search.RefNo == null || search.RefNo.Contains(x.WfProcess.RefNo))
                && x.StatusId > 1 && x.StatusId != 12 
                && (!search.Statuses.Any() || search.Statuses.Contains(x.StatusId))
                && (search.Types == null || search.Types.Contains(x.WfProcess.Workflow.TypeId))
                && (search.Projects == null || search.Projects.Contains(x.WfProcess.RequestedFor.SchoolId))
                );

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
                _refreshLock.Release();
            }
        }

        public void Dispose()
        {
            _disposed = true;
            WorkflowUpdateNotifier.Updated -= OnWorkflowUpdatedAsync;
        }
    }
}
