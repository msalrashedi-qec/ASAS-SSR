
using Shared.Dtos;
using Shared.Dtos.Workflow;

namespace Web.Components.Pages.WF
{
    public partial class TrackProcessTasks
    {

        private bool isLoading = false;

        public IEnumerable<BasicDto> types = new List<BasicDto>();
        public IEnumerable<BasicByteDto> statuses = new List<BasicByteDto>();

        private IEnumerable<InboxDto>? workflows;
        private int TotalPages { get; set; }
        private int PageSize { get; set; } = 50;
        private int PageIndex { get; set; } = 1;
        private int totalRows;

        IList<int> selectedTypes = new int[] { };
        IList<int> selectedStatuses = new int[] { 2 };


        private WorkflowFilterDto search = new();

        protected async override Task OnInitializedAsync()
        {
            try
            {
                isLoading = true;

                types = _mapper.Map<IEnumerable<BasicDto>>(await _uow.WorkflowTypes.GetAllAsync());
                statuses = _mapper.Map<IEnumerable<BasicByteDto>>(await _uow.WorkflowStatuses.FindAllAsync(x => x.Id > 1 && x.Id != 12));

                await GetListAsync();
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

        private async Task GetListAsync()
        {
            isLoading = true;

            
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

           
            //var userRoles = (await _uow.UserRoles.FindAllAsync(x => x.UserId == GetUserId())).Select(x => x.RoleId).ToList();
            //var permittedOus = (await _uow.RoleOus.FindAllAsync(x => userRoles.Contains(x.RoleId))).Select(x => x.OuId).ToList();

            var list = await _uow.WorkflowProcessTasks.FindAllAsync(x => x.StatusId > 1 && x.StatusId != 12
            //&& permittedOus.Contains(x.WfProcess.RequestedFor.OrganizationId)
            && (search.RefNo == null || search.RefNo.Contains(x.WfProcess.RefNo))
            && (search.Statuses == null || search.Statuses.Contains(x.StatusId))
            && (search.Types == null || search.Types.Contains(x.WfProcess.Workflow.TypeId))
            , new[] { "Status", "WfTask", "WfProcess", "WfProcess.RequestedFor", "WfProcess.Workflow.Type" }
            , (PageIndex - 1) * PageSize, PageSize
            , x => x.WfProcess.RefNo, "DESC");

            PagedListResponse<InboxDto> dtoList = new()
            {
                DataList = _mapper.Map<IEnumerable<InboxDto>>(list),
                RowsCount = await _uow.WorkflowProcessTasks.CountAsync(x => x.StatusId > 1 && x.StatusId != 12
               //  && permittedOus.Contains(x.WfProcess.RequestedFor.OrganizationId)
                && (search.RefNo == null || search.RefNo.Contains(x.WfProcess.RefNo))
                && (search.Statuses == null || search.Statuses.Contains(x.StatusId))
                && (search.Types == null || search.Types.Contains(x.WfProcess.Workflow.TypeId)))
            };
            totalRows = dtoList.RowsCount;
            workflows = dtoList.DataList;
            TotalPages = totalRows / PageSize;
            if (totalRows % PageSize > 0)
                TotalPages++;

            isLoading = false;
        }
    }
}