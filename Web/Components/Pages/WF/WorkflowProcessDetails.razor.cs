using Core.Entities.Workflow;
using Core.Enums;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Shared.Dtos.Workflow;
using Web.Extensions;

namespace Web.Components.Pages.WF
{
    public partial class WorkflowProcessDetails
    {
        [Parameter]
        public Guid? Id { get; set; }

        [Parameter]
        public Guid? ProcessId { get; set; }



        private WorkflowProcessDto process = new();
        private WorkflowActionDto action = new();
        public IEnumerable<BasicGuidDto> previousTasks;
        private bool isLoading = false;
        private bool isProcessing = false;

        protected async override Task OnInitializedAsync()
        {
            try
            {
                isLoading = true;
                var task = await _uow.WorkflowProcessTasks.FindAsync(x => x.Id == Id
                    , new[] { "WfProcess.Status", "WfProcess", "WfProcess.Workflow", "WfTask", "WfTask.WorkflowPermissions" });
                if (task == null)
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["NotFound"]);
                    return;
                }
                process = _mapper.Map<WorkflowProcessDto>(task);
                process.Permissions = _mapper.Map<IEnumerable<BasicDto>>(task.WfTask.WorkflowPermissions);
                var histories = (await _uow.WorkflowHistories.FindAllAsync(x => x.WfProcessId == task.WfProcessId
                , new[] { "Creator", "Action", "Task", "Attachments" })).OrderByDescending(x => x.CreatedOn);

                List<WorkflowHistoryDto> mappedHistories = new();
                foreach (var history in histories)
                {
                    var files = history.Attachments.ToList();
                    var h = new WorkflowHistoryDto()
                    {
                        Id = history.Id,
                        ActionTypeId = history.ActionId,
                        ActionTypeName = history.Action.Name,
                        TaskName = history.Task.Name,
                        Comments = history.Comments,
                        UserName = history.Creator.Name,
                        CreatedOn = history.CreatedOn
                    };
                   
                    foreach (var file in files)
                    {
                        h.Attachments.Add(new FileDto { FileName = file.FileName, FilePath = file.FilePath });
                    }

                    mappedHistories.Add(h);
                }
                process.Histories = mappedHistories;
                isLoading = false;
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

        private async Task GetPreviousTasksAsync()
        {
            try
            {
                isLoading = true;
                previousTasks = new List<BasicGuidDto>();
                var wfTask = await _uow.WorkflowProcessTasks.FindAsync(x => x.Id == process.TaskId, new[] { "WfTask" });
                if (wfTask != null)
                {
                    var list = await _uow.WorkflowProcessTasks.FindAllAsync(x => x.WfProcessId == wfTask.WfProcessId && x.WfTask.SN < wfTask.WfTask.SN && x.StatusId == 10
                        , new[] { "WfTask" }, x => x.WfTask.SN, OrderBy.Descending);

                    if (list.Any() && !await _uow.WorkflowPermissions.Existing(x => x.WorkflowTaskId == wfTask.WfTaskId && x.PermissionId == 1005))
                    {
                        var firstTask = list.First();
                        list = list.Where(x => x.Id == firstTask.Id);
                    }
                    previousTasks = _mapper.Map<IEnumerable<BasicGuidDto>>(list);
                }
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
        private async Task ApproveAsync()
        {
            if (action.Attachments.Any() && action.Attachments.Sum(x => x.FileSize) > 25)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["AttachmentsSizeExceeded25MB"]);
                return;
            }
            isProcessing = true;
            action.TaskId = Guid.Parse(process.TaskId.ToString());

            var wfTask = await _uow.WorkflowProcessTasks.FindAsync(x => x.Id == action.TaskId && x.WfProcess.StatusId == 2, new[] { "WfTask", "WfProcess", "WfProcess.Workflow" });
            if (wfTask == null)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["RequestDetailsNotFound"]);
                return;
            }
            
            if (wfTask.StatusId != 2)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["TaskNotActive"]);
                return;
            }

            DateTime approvalTime = DateTime.UtcNow.GetKsaDateTime();
            string approvedBy = await _userService.GetUserIdAsync();

            wfTask.StatusId = 10;
            wfTask.EndDate = approvalTime;

            var history = new WorkflowHistory
            {
                WfProcessId = wfTask.WfProcessId,
                ActionId = 2,
                TaskId = wfTask.WfTaskId,
                Comments = action.Comments,
                CreatorId = approvedBy,
                CreatedOn = approvalTime
            };

            foreach (var file in action.Attachments)
            {
                if (!string.IsNullOrEmpty(file.FilePath))
                {
                    history.Attachments.Add(new()
                    {
                        FilePath = file.FilePath,
                        FileName = file.FileName,
                    });
                }
            }

            await _uow.WorkflowHistories.AddAsync(history);

            if (!await _uow.WorkflowProcessTasks.Existing(x => x.WfProcessId == wfTask.WfProcessId && x.Id != wfTask.Id && x.WfTask.SN == wfTask.WfTask.SN && x.StatusId == 2))
            {
                if (wfTask.WfTask.IsTheEnd)
                {
                    wfTask.WfProcess.StatusId = 10;
                    if(wfTask.WfProcess.WorkflowId == 1) // workflow is for discount request
                    {
                        var discount = await _uow.DiscountRequests.FindAsync(x => x.WfProcessId == wfTask.WfProcessId, ["Details", "Student", "Discount"]);
                        if (discount != null)
                        {
                            foreach (var detail in discount.Details)
                            {
                                await _uow.StudentAccounts.AddAsync(new StudentAccount
                                {
                                    ContractId = discount.Student.CurrentContractId,
                                    TransactionTypeId = discount.Discount.TransactionTypeId,
                                    Date = approvalTime.Date,
                                    ServiceId = detail.ServiceId.Value,
                                    Categoryid = detail.CategoryId.Value,
                                    SemesterId = detail.SemesterId,
                                    Credit = detail.Amount > 0 ? detail.Amount : 0,
                                    Debit = detail.Amount < 0 ? Math.Abs(detail.Amount) : 0,
                                    Percentage = (decimal)detail.Percentage,
                                    Comments = $"{discount.Discount.NameAr} {(detail.Percentage > 0 ? $"{detail.Percentage}%" : "")}",
                                    CreatedBy = approvedBy,
                                    CreatedOn = approvalTime
                                });
                            }
                        }
                    }
                }
                else
                {
                    int skipFromTaskId = 0;
                    int skipToTaskId = 1;
                    int sharedTasksCount = 0;

                    var nextWfTasks = (await _uow.WorkflowProcessTasks.FindAllAsync(x => x.WfProcessId == wfTask.WfProcessId && x.WfTask.SN > wfTask.WfTask.SN, new[] { "WfTask" }, x => x.WfTask.SN)).ToList();
                    if (nextWfTasks.Any())
                        skipFromTaskId = skipToTaskId = nextWfTasks.First().WfTask.SN;

                    foreach (var task in nextWfTasks)
                    {
                        if (task.WfTask.SN > skipToTaskId) break;

                        if (task.WfTask.SN < skipToTaskId && task.WfTask.SN > skipFromTaskId)
                        {
                            task.StartDate = task.EndDate = approvalTime;
                            task.StatusId = 12;
                        }
                        else if (task.WfTask.SN == skipToTaskId || task.WfTask.SN == skipFromTaskId)
                        {
                            if (task.WfTask.SN == skipFromTaskId || sharedTasksCount > 0 || (task.WfTask.SN == skipToTaskId && sharedTasksCount == 0))
                            {
                                task.StartDate = approvalTime;
                                task.EndDate = task.StartDate.Value.AddDays(task.WfTask.RequiredDays);
                                task.StatusId = 2;

                                var sharing = await _uow.WorkflowFutureSharings.FindAsync(x => x.WorkflowTaskId == task.WfTaskId
                                            && x.ShareStartDate.Date <= approvalTime.Date && x.ShareEndDate.Date >= approvalTime.Date, new[] { "WorkflowTask" });

                                if (sharing != null)
                                {
                                    task.SharedWithUserId = sharing.SharedWithUserId;
                                    task.ShareStartDate = task.StartDate;
                                    task.ShareEndDate = sharing.ShareEndDate;

                                    await _uow.WorkflowHistories.AddAsync(new WorkflowHistory
                                    {
                                        WfProcessId = task.WfProcessId,
                                        ActionId = 10,
                                        TaskId = task.WfTaskId,
                                        Comments = sharing.Comments,
                                        CreatorId = sharing.CreatedBy,
                                        CreatedOn = approvalTime
                                    });
                                }
                                if (task.StatusId == 2)
                                    skipToTaskId = task.WfTask.SN;
                            }
                        }

                        if (task.WfTask.SN == skipFromTaskId) sharedTasksCount++;
                        else if (task.WfTask.SN <= skipToTaskId) sharedTasksCount = 0;
                    }

                    if (nextWfTasks.Any())
                        await _uow.WorkflowProcessTasks.UpdateRange(nextWfTasks);
                }
            }

            await _uow.WorkflowProcessTasks.Update(wfTask);
            await _uow.WorkflowProcesses.Update(wfTask.WfProcess);

            if (await _uow.SaveAsync())
            {
                _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                _navigator.NavigateTo("inbox");
            }
            else
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["Faild_to_save"]);

            isProcessing = false;
        }

        private async Task ReWorkAsync()
        {
            if (string.IsNullOrEmpty(action.Comments) || action.ReturnTaskId == null || action.ReturnTaskId == Guid.Empty)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["SelectReturnToAndComments"]);
                return;
            }

            if (action.Attachments.Any() && action.Attachments.Sum(x => x.FileSize) > 25)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["AttachmentsSizeExceeded25MB"]);
                return;
            }

            try
            {
                isProcessing = true;

                var process = await _uow.WorkflowProcesses.FindAsync(x => x.Id == action.ProcessId && x.StatusId == 2);
                if (process == null)
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["TaskNotActive"]);
                    return;
                }

                var processTasks = await _uow.WorkflowProcessTasks.FindAllAsync(x => x.WfProcessId == process.Id, new[] { "WfTask" });
                var currentTasks = processTasks.Where(x => x.StatusId == 2).ToList();
                if (!currentTasks.Any(x => x.Id == action.TaskId))
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["TaskNotActive"]);
                    return;
                }

                var currentTask = currentTasks.First(x => x.Id == action.TaskId);

                List<WorkflowProcessTask> affectedTasks = new();
                foreach (var tsk in currentTasks)
                {
                    tsk.StatusId = 1;
                    tsk.StartDate = null;
                    tsk.EndDate = null;
                    affectedTasks.Add(tsk);
                }
                await _uow.WorkflowProcessTasks.UpdateRange(affectedTasks);

                var history = new WorkflowHistory
                {
                    WfProcessId = process.Id,
                    ActionId = 3,
                    TaskId = currentTask.WfTaskId,
                    Comments = action.Comments,
                    CreatorId = await _userService.GetUserIdAsync(),
                    CreatedOn = DateTime.UtcNow.GetKsaDateTime()
                };

                foreach (var file in action.Attachments)
                {
                    if (!string.IsNullOrEmpty(file.FilePath))
                    {
                        history.Attachments.Add(new()
                        {
                            FilePath = file.FilePath,
                            FileName = file.FileName,
                        });
                    }
                }

                var returnTask = processTasks.FirstOrDefault(x => x.Id == action.ReturnTaskId);
                if (returnTask != null)
                {
                    DateTime returnTime = DateTime.UtcNow.GetKsaDateTime();

                    returnTask.StatusId = 2;
                    returnTask.StartDate = returnTime;
                    returnTask.EndDate = returnTask.StartDate.Value.AddDays(returnTask.WfTask.RequiredDays);


                    var sharing = await _uow.WorkflowFutureSharings.FindAsync(x => x.WorkflowTaskId == returnTask.WfTaskId
                                && x.ShareStartDate.Date <= returnTime.Date && x.ShareEndDate.Date >= returnTime.Date, new[] { "WorkflowTask" });
                    if (sharing != null)
                    {
                        //var employee = await _uow.Employees.FindAsync(x => x.Id == process.RequestedForId, new[] { "Ou" });
                        if (sharing != null)
                        {
                            returnTask.SharedWithUserId = sharing.SharedWithUserId;
                            returnTask.ShareStartDate = returnTask.StartDate;
                            returnTask.ShareEndDate = sharing.ShareEndDate;

                            await _uow.WorkflowHistories.AddAsync(new WorkflowHistory
                            {
                                WfProcessId = returnTask.WfProcessId,
                                ActionId = 10,
                                TaskId = returnTask.WfTaskId,
                                Comments = sharing.Comments,
                                CreatorId = sharing.CreatedBy,
                                CreatedOn = returnTime
                            });
                        }
                    }

                    await _uow.WorkflowProcessTasks.Update(returnTask);

                    history.Comments = $"{returnTask.WfTask.Name} --- {action.Comments}";
                }

                await _uow.WorkflowHistories.AddAsync(history);
                await _uow.WorkflowProcesses.Update(process);
                if (await _uow.SaveAsync())
                {
                    _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                    await _js.InvokeVoidAsync("APP.hideModal", "ReworkModal");
                    _navigator.NavigateTo("inbox");
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
                await _js.InvokeVoidAsync("APP.hideModal", "ReworkModal");
                isProcessing = false;
            }
        }
        private async Task RejectAsync()
        {
            if (string.IsNullOrEmpty(action.Comments))
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["EnterReason"]);
                return;
            }

            if (action.Attachments.Any() && action.Attachments.Sum(x => x.FileSize) > 25)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["AttachmentsSizeExceeded25MB"]);
                return;
            }

            try
            {
                isProcessing = true;
                var wfTask = await _uow.WorkflowProcessTasks.FindAsync(x => x.Id == action.TaskId && x.WfProcess.StatusId == 2, new[] { "WfTask", "WfProcess", "WfProcess.Workflow" });
                if (wfTask == null)
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["RequestDetailsNotFound"]);
                    return;
                }
                if (wfTask.StatusId != 2)
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["TaskNotActive"]);
                    return;
                }

                wfTask.StatusId = 15;
                wfTask.EndDate = DateTime.UtcNow.GetKsaDateTime();
                wfTask.WfProcess.StatusId = 15;
                await _uow.WorkflowProcessTasks.Update(wfTask);

                var history = new WorkflowHistory
                {
                    WfProcessId = wfTask.WfProcessId,
                    ActionId = 4,
                    TaskId = wfTask.WfTaskId,
                    Comments = action.Comments,
                    CreatorId = await _userService.GetUserIdAsync(),
                    CreatedOn = DateTime.UtcNow.GetKsaDateTime()
                };

                foreach (var file in action.Attachments)
                {
                    if (!string.IsNullOrEmpty(file.FilePath))
                    {
                        history.Attachments.Add(new()
                        {
                            FilePath = file.FilePath,
                            FileName = file.FileName,
                        });
                    }
                }
                await _uow.WorkflowHistories.AddAsync(history);

                var nextTasks = await _uow.WorkflowProcessTasks.FindAllAsync(x => x.WfProcessId == wfTask.WfProcessId && x.Id != wfTask.Id && x.StatusId == 2);
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

                await _uow.WorkflowProcesses.Update(wfTask.WfProcess);
                if (await _uow.SaveAsync())
                {
                    _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                    await _js.InvokeVoidAsync("APP.hideModal", "RejectModal");
                    _navigator.NavigateTo("inbox");

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
                await _js.InvokeVoidAsync("APP.hideModal", "RejectModal");
                isProcessing = false;
            }
        }
        private async Task CancelAsync()
        {
            if (string.IsNullOrEmpty(action.Comments))
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["EnterReason"]);
                return;
            }

            if (action.Attachments.Any() && action.Attachments.Sum(x => x.FileSize) > 25)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["AttachmentsSizeExceeded25MB"]);
                return;
            }
            try
            {
                isProcessing = true;
                var wfTask = await _uow.WorkflowProcessTasks.FindAsync(x => x.Id == action.TaskId && x.WfProcess.StatusId == 2, new[] { "WfTask", "WfProcess", "WfProcess.Workflow" });
                if (wfTask == null)
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["RequestDetailsNotFound"]);
                    return;
                }

                if (wfTask.StatusId != 2)
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["TaskNotActive"]);
                    return;
                }

                wfTask.StatusId = 20;
                wfTask.EndDate = DateTime.UtcNow.GetKsaDateTime();
                wfTask.WfProcess.StatusId = 20;
                await _uow.WorkflowProcessTasks.Update(wfTask);

                var history = new WorkflowHistory
                {
                    WfProcessId = wfTask.WfProcessId,
                    ActionId = 5,
                    TaskId = wfTask.WfTaskId,
                    Comments = action.Comments,
                    CreatorId = await _userService.GetUserIdAsync(),
                    CreatedOn = DateTime.UtcNow.GetKsaDateTime()
                };

                foreach (var file in action.Attachments)
                {
                    if (!string.IsNullOrEmpty(file.FilePath))
                    {
                        history.Attachments.Add(new()
                        {
                            FilePath = file.FilePath,
                            FileName = file.FileName,
                        });
                    }
                }
                await _uow.WorkflowHistories.AddAsync(history);

                var nextTasks = await _uow.WorkflowProcessTasks.FindAllAsync(x => x.WfProcessId == wfTask.WfProcessId && x.Id != wfTask.Id && x.StatusId == 2);
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


                await _uow.WorkflowProcesses.Update(wfTask.WfProcess);
                if (await _uow.SaveAsync())
                {
                    _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                    await _js.InvokeVoidAsync("APP.hideModal", "CancelModal");
                    _navigator.NavigateTo("inbox");
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
                await _js.InvokeVoidAsync("APP.hideModal", "CancelModal");
                isProcessing = false;
            }
        }
        private async Task UploadFile(InputFileChangeEventArgs e)
        {
            foreach (var file in e.GetMultipleFiles())
            {
                try
                {
                    if (!FileValidator.ValidateSize(file.Size, 25))
                    {
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["FileExceeded25MB"]);
                        return;
                    }
                    if (!FileValidator.ValidateType(file.Name))
                    {
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["FileNotSupported"]);
                        return;
                    }
                    string newFileName = $"W_{Path.GetRandomFileName()}{Path.GetExtension(file.Name).ToLowerInvariant()}";
                    var uploadDirectory = Path.Combine(_env.WebRootPath, "uploads", "workflow");
                    Directory.CreateDirectory(uploadDirectory);
                    var fullPath = Path.Combine(uploadDirectory, newFileName);

                    using (var stream = file.OpenReadStream(file.Size))
                    {
                        using (var fileStream = File.Create(fullPath))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                    action.Attachments.Add(new FileDto
                    {
                        FileName = file.Name,
                        FilePath = newFileName,
                        FileSize = file.Size / 1024 / 1024,
                        IsNew = true
                    });
                }
                catch (Exception ex)
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer[ex.Message]);
                }
            }
        }

    }
}