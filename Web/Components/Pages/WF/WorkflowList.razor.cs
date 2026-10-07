
using Core.Entities.Workflow;
using Shared.Dtos.Workflow;
using Web.Extensions;

namespace Web.Components.Pages.WF
{
    public partial class WorkflowList
    {

        private IEnumerable<BasicDto> wfTypes = new List<BasicDto>();
        private IEnumerable<WorkflowDto> workFlows = new List<WorkflowDto>();
        private WorkflowDto wf = new();

        private bool isLoading = false;
        private bool isProcessing = false;

        protected async override Task OnInitializedAsync()
        {
            await GetListAsync();
            await GetTypesAsync();
        }

        private async Task GetListAsync()
        {
            try
            {
                isLoading = true;
                var list = await _uow.Workflows.FindAllAsync(x => x.Id > 0, new[] { "Type" });
                workFlows = _mapper.Map<IEnumerable<WorkflowDto>>(list.OrderBy(x => x.TypeId));
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
        private async Task GetTypesAsync()
        {
            try
            {
                isLoading = true;
                wfTypes = _mapper.Map<IEnumerable<BasicDto>>(await _uow.WorkflowTypes.GetAllAsync());
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
        private void New()
        {
            wf = new WorkflowDto();
        }

        private void Edit(WorkflowDto item)
        {
            wf = item;
        }

        private async Task SaveDataAsync()
        {
            if (wf != null && wf.Id > 0)
            {
                try
                {
                    isProcessing = true;
                    if (await _uow.Workflows.Existing(x => x.Version.ToLower() == wf.Version.ToLower() && x.Id != wf.Id))
                    {
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["ThisNameExisting"]);
                        return;
                    }

                    var workflow = await _uow.Workflows.FindAsync(x => x.Id == wf.Id);
                    if (workflow == null)
                    {
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["NotFound"]);
                        return;
                    }
                    _mapper.Map(wf, workflow);
                    workflow.UpdatedBy = await _userService.GetUserIdAsync();
                    workflow.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();
                   
                    await _uow.Workflows.Update(workflow);
                    if (await _uow.SaveAsync())
                    {
                        _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                        await GetListAsync();
                    }
                    else
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
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
            else if (wf != null)
            {
                try
                {
                    isProcessing = true;
                    if (await _uow.Workflows.Existing(x => x.Version.ToLower() == wf.Version.ToLower()))
                    {
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["ThisNameExisting"]);
                        return;
                    }

                    // Map the comming data to the correct format
                    var workflow = _mapper.Map<Workflow>(wf);
                    workflow.CreatedOn = DateTime.UtcNow.GetKsaDateTime();
                    workflow.CreatedBy = await _userService.GetUserIdAsync();

                    await _uow.Workflows.AddAsync(workflow);
                    if (await _uow.SaveAsync())
                    {
                        _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                        await GetListAsync();
                    }
                    else
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
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
        }
    }
}