using Shared.Dtos.Workflow;

namespace Web.Components.Pages.WF
{
    public partial class WorkflowProcessAging
    {
        [Inject]
        public RoleManager<ApplicationRole> RoleManager { get; set; }
        
        [Inject]
        public UserManager<ApplicationUser> UserManager { get; set; }



        private bool isLoading = false;


        public IEnumerable<BasicDto> types = new List<BasicDto>();
        public IEnumerable<BasicByteDto> statuses = new List<BasicByteDto>();

        private List<WorkflowAgingDto> workflows = new();
        private IEnumerable<InboxDto> wfTasks = new List<InboxDto>();
        private Guid? processId;
        private byte sortColumn = 0;
        private string orderBy = "ASC";

        IList<int> selectedTypes = new int[] { };
        IList<int> selectedStatuses = new int[] { 2 };


        private WorkflowFilterDto filter = new();

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

                //Type
                if (selectedTypes.Count > 0)
                {
                    filter.Types = new List<int>();
                    foreach (int type in selectedTypes)
                    {
                        filter.Types.Add(type);
                    }
                }
                else
                    filter.Types = null;

                //Status
                if (selectedStatuses.Count > 0)
                {
                    filter.Statuses = new List<byte>();
                    foreach (byte status in selectedStatuses)
                    {
                        filter.Statuses.Add(status);
                    }
                }
                else
                    filter.Statuses = null;
                var allRoles = RoleManager.Roles.ToList();


                var list = await _uow.WorkflowProcessTasks.FindAllAsync(x => !x.WfProcess.IsDeleted
                && (filter.RefNo == null || x.WfProcess.RefNo == filter.RefNo)
                && (filter.Statuses == null || filter.Statuses.Contains(x.StatusId))
                && (filter.Types == null || filter.Types.Contains(x.WfProcess.Workflow.TypeId))
                , new[] { "Status", "WfTask", "WfProcess", "WfProcess.RequestedFor", "WfProcess.RequestedFor.Organization", "WfProcess.Workflow.Type" });

                var types = list.Select(x => new { x.WfProcess.Workflow.TypeId, x.WfProcess.Workflow.Type.Name }).Distinct().OrderBy(x => x.TypeId);

                workflows = new List<WorkflowAgingDto>();

                foreach (var item in types)
                {
                    var task = new WorkflowAgingDto();
                    task.TypeId = item.TypeId;
                    task.TypeName = item.Name;
                    task.From0To7 = list.Count(x => x.WfProcess.Workflow.TypeId == item.TypeId && (int)(DateTime.UtcNow.GetKsaDateTime() - x.StartDate.Value).TotalDays < 7);
                    task.From8To30 = list.Count(x => x.WfProcess.Workflow.TypeId == item.TypeId && (int)(DateTime.UtcNow.GetKsaDateTime() - x.StartDate.Value).TotalDays >= 7 && (int)(DateTime.UtcNow.GetKsaDateTime() - x.StartDate.Value).TotalDays <= 30);
                    task.From31To60 = list.Count(x => x.WfProcess.Workflow.TypeId == item.TypeId && (int)(DateTime.UtcNow.GetKsaDateTime() - x.StartDate.Value).TotalDays > 30 && (int)(DateTime.UtcNow.GetKsaDateTime() - x.StartDate.Value).TotalDays <= 60);
                    task.Over60 = list.Count(x => x.WfProcess.Workflow.TypeId == item.TypeId && (int)(DateTime.UtcNow.GetKsaDateTime() - x.StartDate.Value).TotalDays > 60);

                    var list0To7 = list.Where(x => x.WfProcess.Workflow.TypeId == item.TypeId && (int)(DateTime.UtcNow.GetKsaDateTime() - x.StartDate.Value).TotalDays < 7);
                    foreach (var tsk in list0To7)
                    {
                        var newTask = _mapper.Map<InboxDto>(tsk);
                        var role = allRoles.FirstOrDefault(x => x.Id == tsk.WfTask.ResponsibleRoleId);
                        var responsibles = await UserManager.GetUsersInRoleAsync(role.Name);
                        //var responsibles = allRoles.Where(x => x.Id == tsk.ResponsibleRoleId && x.User.IsActive, new[] { "User" });
                        if (responsibles.Any())
                        {
                            foreach (var user in responsibles.Where(x => x.IsActive))
                            {
                                BasicDto responsible = new()
                                {
                                    Name = user.Name,
                                };
                                newTask.Responsibles.Add(responsible);
                            }
                        }
                        task.From0To7Tasks.Add(newTask);
                    }

                    var list8To30 = list.Where(x => x.WfProcess.Workflow.TypeId == item.TypeId && (int)(DateTime.UtcNow.GetKsaDateTime() - x.StartDate.Value).TotalDays >= 7 && (int)(DateTime.UtcNow.GetKsaDateTime() - x.StartDate.Value).TotalDays <= 30);
                    foreach (var tsk in list8To30)
                    {
                        var newTask = _mapper.Map<InboxDto>(tsk);
                        var role = allRoles.FirstOrDefault(x => x.Id == tsk.WfTask.ResponsibleRoleId);
                        var responsibles = await UserManager.GetUsersInRoleAsync(role.Name);
                        //var responsibles = await _uow.UserRoles.FindAllAsync(x => x.RoleId == tsk.ResponsibleRoleId && x.User.Employee.StatusId >= 10 && x.User.Employee.StatusId <= 30, new[] { "User", "User.Employee" });
                        if (responsibles.Any())
                        {
                            foreach (var user in responsibles.Where(x => x.IsActive))
                            {
                                BasicDto responsible = new()
                                {
                                    Name = user.Name,
                                };
                                newTask.Responsibles.Add(responsible);
                            }
                        }
                        task.From8To30Tasks.Add(newTask);
                    }

                    var list31To60 = list.Where(x => x.WfProcess.Workflow.TypeId == item.TypeId && (int)(DateTime.UtcNow.GetKsaDateTime() - x.StartDate.Value).TotalDays > 30 && (int)(DateTime.UtcNow.GetKsaDateTime() - x.StartDate.Value).TotalDays <= 60);
                    foreach (var tsk in list31To60)
                    {
                        var newTask = _mapper.Map<InboxDto>(tsk);
                        var role = allRoles.FirstOrDefault(x => x.Id == tsk.WfTask.ResponsibleRoleId);
                        var responsibles = await UserManager.GetUsersInRoleAsync(role.Name);
                        if (responsibles.Any())
                        {
                            foreach (var user in responsibles.Where(x => x.IsActive))
                            {
                                BasicDto responsible = new()
                                {
                                    Name = user.Name,
                                };
                                newTask.Responsibles.Add(responsible);
                            }
                        }

                        task.From31To60Tasks.Add(newTask);
                    }

                    var listOver60 = list.Where(x => x.WfProcess.Workflow.TypeId == item.TypeId && (int)(DateTime.UtcNow.GetKsaDateTime() - x.StartDate.Value).TotalDays > 60);
                    foreach (var tsk in listOver60)
                    {
                        var newTask = _mapper.Map<InboxDto>(tsk);
                        var role = allRoles.FirstOrDefault(x => x.Id == tsk.WfTask.ResponsibleRoleId);
                        var responsibles = await UserManager.GetUsersInRoleAsync(role.Name);

                        if (responsibles.Any())
                        {
                            foreach (var user in responsibles.Where(x => x.IsActive))
                            {
                                BasicDto responsible = new()
                                {
                                    Name = user.Name,
                                };
                                newTask.Responsibles.Add(responsible);
                            }
                        }
                        task.Over60Tasks.Add(newTask);
                    }

                    workflows.Add(task);
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
        private void SortData(byte columnId)
        {
            if (wfTasks == null) return;

            if (columnId == sortColumn && orderBy == "ASC")
                orderBy = "DESC";
            else if (columnId == sortColumn && orderBy == "DESC")
                orderBy = "ASC";

            sortColumn = columnId;

            if (columnId == 1 && orderBy == "ASC")
                wfTasks = wfTasks.OrderBy(x => x.WfProcessId).ToList();
            else if (columnId == 1)
                wfTasks = wfTasks.OrderByDescending(x => x.WfProcessId).ToList();

            else if (columnId == 3 && orderBy == "ASC")
                wfTasks = wfTasks.OrderBy(x => x.RequesterName).ToList();
            else if (columnId == 3)
                wfTasks = wfTasks.OrderByDescending(x => x.RequesterName).ToList();

            else if (columnId == 5 && orderBy == "ASC")
                wfTasks = wfTasks.OrderBy(x => x.TaskName).ToList();
            else if (columnId == 5)
                wfTasks = wfTasks.OrderByDescending(x => x.TaskName).ToList();

            else if (columnId == 6 && orderBy == "ASC")
                wfTasks = wfTasks.OrderBy(x => x.StartDate).ToList();
            else if (columnId == 6)
                wfTasks = wfTasks.OrderByDescending(x => x.StartDate).ToList();
        }
    }
}
