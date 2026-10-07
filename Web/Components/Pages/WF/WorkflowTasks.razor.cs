
using Core.Entities.Workflow;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos.Workflow;
using Web.Extensions;

namespace Web.Components.Pages.WF
{
    public partial class WorkflowTasks
    {
        [Parameter]
        public int Id { get; set; }

        [Inject]
        public RoleManager<ApplicationRole> RoleManager { get; set; }


        private IEnumerable<WorkflowTaskDto> tasks = new List<WorkflowTaskDto>();
        private IEnumerable<ApplicationRole> roles = new List<ApplicationRole>();
        private IEnumerable<BasicDto> conditions = new List<BasicDto>();

        private WorkflowTaskDto task = new();
        private BasicDto wf = new();

        private bool isLoading = false;
        private bool isProcessing = false;

        protected async override Task OnInitializedAsync()
        {
            isLoading = true;
            await GetWorkFlowAsync();
            await GetTasksAsync();
            await GetDropDownListsAsync();
            isLoading = false;
        }

        public async Task GetTasksAsync()
        {
            var list = await _uow.WorkflowTasks.FindAllAsync(x => x.WorkflowId == Id, x => x.SN);
            tasks = _mapper.Map<IEnumerable<WorkflowTaskDto>>(list);
        }
        public async Task GetDropDownListsAsync()
        {
            roles = await RoleManager.Roles.ToListAsync();
            conditions = _mapper.Map<IEnumerable<BasicDto>>(await _uow.WorkflowActions.FindAllAsync(x => x.WorkflowTypeId == 0 || x.WorkflowTypeId == Id));
        }
        public async Task GetWorkFlowAsync()
        {
            var item = await _uow.Workflows.FindAsync(x => x.Id == Id);
            if (item == null) return;
            wf.Id = item.TypeId;
            wf.Name = item.Version;
        }
        private void New()
        {
            task = new WorkflowTaskDto();
            task.SN = tasks.Any() ? tasks.Max(x => x.SN) + 1 : 0;
            task.JumpToTaskId = task.SN;
        }
        private void Edit(WorkflowTaskDto itemToEdit)
        {
            task = itemToEdit;
        }
        private async Task SaveDataAsync()
        {
            isLoading = true;

            if (task != null && task.Id > 0)
            {
                if (await _uow.WorkflowTasks.Existing(x => x.Name.ToLower() == task.Name.ToLower()
                && x.Id != task.Id && x.WorkflowId == task.WorkflowId))
                {
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                    return;
                }
              

                var item = await _uow.WorkflowTasks.FindAsync(x => x.Id == task.Id);
                if (item == null)
                {
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], _localizer["NotFound"]);
                    return;
                }

                _mapper.Map(task, item);
                item.UpdatedBy = await _userService.GetUserIdAsync();
                item.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();
                await _uow.WorkflowTasks.Update(item);
                if (await _uow.SaveAsync())
                {
                    _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                    await GetTasksAsync();
                }
                else
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
            }
            else if (task != null)
            {
                if (await _uow.WorkflowTasks.Existing(x => x.Name.ToLower() == task.Name.ToLower() && x.WorkflowId == task.WorkflowId))
                {
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                    return;
                }

                // Map the comming data to the correct format
                task.WorkflowId = Id;

                var newTask = _mapper.Map<WorkflowTask>(task);
                newTask.CreatedOn = DateTime.UtcNow.GetKsaDateTime();
                newTask.CreatedBy = await _userService.GetUserIdAsync();

                await _uow.WorkflowTasks.AddAsync(newTask);
                if (await _uow.SaveAsync())
                {
                    _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                    await GetTasksAsync();
                    await GetTasksAsync();
                    task.SN = tasks.Max(x => x.SN) + 1;
                    task.JumpToTaskId = task.SN;
                }
                else
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
            }

            isLoading = false;
        }
    }
}
