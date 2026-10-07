
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos.Workflow;

namespace Web.Components.Partial.WF
{
    public partial class TrackRequest
    {
        [Parameter]
        public Guid? ProcessId { get; set; }

        [Inject]
        public UserManager<ApplicationUser> UserManager { get; set; } = null!;

        [Inject]
        public IDbContextFactory<ApplicationDbContext> DbContextFactory { get; set; } = null!;


        private bool isProcessing = false;

        private IEnumerable<TrackProcessDto> tasks = [];

        protected override async Task OnParametersSetAsync()
        {
            await GetTaskAsync();
        }

        private async Task GetTaskAsync()
        {
            if (ProcessId != null)
            {
                try
                {
                    isProcessing = true;
                    await using var context = await DbContextFactory.CreateDbContextAsync();
                    var list = await context.WorkflowProcessTasks
                        .AsNoTracking()
                        .Where(x => x.WfProcessId == ProcessId)
                        .Include(x => x.WfTask)
                        .Include(x => x.Status)
                        .Include(x => x.WfProcess)
                        .OrderBy(x => x.WfTask.SN)
                        .ToListAsync();

                    List<TrackProcessDto> tasksList = new();

                    foreach (var item in list)
                    {
                        var task = _mapper.Map<TrackProcessDto>(item);
                        if (task.StatusId == 2)
                        {
                            //if (item.ResponsibleRoleId == "3")//Requester
                            //{
                            //    var user = await UserManager.FindByIdAsync(item.WfProcess.CreatedBy.ToString());
                            //    if (user is not null)
                            //    {
                            //        BasicDto responsible = new()
                            //        {
                            //            Name = user.Name,
                            //        };
                            //        task.Responsibles.Add(responsible);
                            //    }
                            //}
                            //else
                            //{
                            //    //var responsibles = await _uow.UserRoles.FindAllAsync(x => x.RoleId == task.ResponsibleRoleId && x.User.IsActive, new[] { "User" });
                            //    //if (responsibles.Any())
                            //    //{
                            //    //    foreach (var employee in responsibles)
                            //    //    {
                            //    //        BasicDto responsible = new()
                            //    //        {
                            //    //            Name = employee.User.Name,
                            //    //        };
                            //    //        task.Responsibles.Add(responsible);
                            //    //    }
                            //    //}
                            //}
                        }
                        tasksList.Add(task);
                    }
                    tasks = tasksList;
                }
                catch (Exception ex) 
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer[ex.Message]);
                }
                finally
                {
                    isProcessing = false;
                }
                
            }
        }
    }
}
