using Core.Entities.Discounts;
using Core.Entities.Workflow;
using Shared.Dtos.Discounts;

namespace Web.Components.Pages.Discounts;

public partial class DiscountRequestDetails
{
    [Parameter]
    public Guid? ProcessId { get; set; }

    [Parameter]
    public IEnumerable<BasicDto>? Permissions { get; set; }

    [Parameter]
    public byte? ProcessStatus { get; set; }




    private const long MaxAttachmentsSize = 25L * 1024 * 1024;

    private Student student = new();
    private List<BasicGuidDto> students = [];
    private List<StudentAccount> studentAccounts = [];
    private List<Discount> discountTypes = [];
    private List<BasicByteDto> semesters = [];
    private List<FileDto> attachments = [];
    private Dictionary<DiscountRequestDetailDto, double> configuredDiscountAmounts = [];
    private bool IsSettlement => discountTypes.Any(x => x.Id == _model.DiscountId && x.TransactionTypeId == Core.Enums.TransactionTypeIds.FinancialSettlement);
    private DiscountRequestDto _model = new();
    private bool isLoading;
    private bool isSaving;
    private bool canEdit = false;

    private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
    private string DiscountTextProperty => IsArabic ? nameof(Discount.NameAr) : nameof(Discount.Name);
    private string StudentStatus => student?.CurrentContract?.Status is null
        ? "-"
        : IsArabic ? student.CurrentContract.Status.NameAr : student.CurrentContract.Status.Name;
    private string StudentLevelAndClass => student?.CurrentContract?.Class is null
        ? "-"
        : IsArabic
            ? $"{student.CurrentContract.Class.Level.NameAr} - {student.CurrentContract.Class.NameAr}"
            : $"{student.CurrentContract.Class.Level.Name} - {student.CurrentContract.Class.Name}";
    private decimal RemainingBalance => decimal.Round(studentAccounts.Sum(x => (decimal)x.Debit - (decimal)x.Credit), 2);
    private double TotalDiscount => Math.Round(_model.Details.Where(x => x.IsSelected).Sum(x => x.Amount), 2);


    protected override async Task OnParametersSetAsync()
    {
        canEdit = ProcessId == null;
        if (ProcessId != null && ProcessId != Guid.Empty)
            await GetDetailsAsync();
    }

    protected override async Task OnInitializedAsync()
    {
        isLoading = true;
        try
        {
            await _appStateService.InitializeAsync();
            if (_appStateService.SchoolId == Guid.Empty)
                return;

            var schoolStudents = await _uow.Students.FindAllAsync(x => x.SchoolId == _appStateService.SchoolId, ["School"], x => x.SN);
            students = _mapper.Map<List<BasicGuidDto>>(schoolStudents);
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

    private async Task GetDetailsAsync()
    {
        try
        {
            isLoading = true;
            canEdit = ProcessId == null || (Permissions != null && Permissions.Any(x => x.Id == 1004) && ProcessStatus != null && ProcessStatus == 2);
            var request = await _uow.DiscountRequests.FindAsync(x => x.WfProcessId == ProcessId,
                ["Details", "Details.Semester", "Discount", "WfProcess", "WfProcess.Status"]);
            if (request is null)
            {
                _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                return;
            }

            _model = _mapper.Map<DiscountRequestDto>(request);
            await LoadStudentAsync();
            if (student is not null)
                students = [_mapper.Map<BasicGuidDto>(student)];
            // The reason and uploaded files belong to the workflow submission/re-submission,
            // rather than to DiscountRequest itself. Restore the latest submitted values when
            // the request is opened for review.
            var submissionHistory = (await _uow.WorkflowHistories.FindAllAsync(x => x.WfProcessId == ProcessId && (x.ActionId == 1 || x.ActionId == 10), ["Attachments"]))
                .OrderByDescending(x => x.CreatedOn)
                .FirstOrDefault();

            if (submissionHistory is not null)
            {
                _model.Comments = submissionHistory.Comments;
                attachments = submissionHistory.Attachments.Select(x => new FileDto
                {
                    FileName = x.FileName,
                    FilePath = x.FilePath
                }).ToList();
                _model.Attachments = attachments;
            }

            foreach (var detail in _model.Details)
                detail.RemainingAmount = decimal.Round(studentAccounts.Where(x => x.ContractId == detail.ContractId
                    && x.ServiceId == detail.ServiceId && x.Categoryid == detail.CategoryId && x.SemesterId == detail.SemesterId)
                    .Sum(x => (decimal)x.Debit - (decimal)x.Credit), 2);
            configuredDiscountAmounts = _model.Details.Where(x => x.HasConfiguredPercentage).ToDictionary(x => x, x => x.Amount);
            if (canEdit) SynchronizeTaxDiscounts();
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

    private async Task LoadStudentAsync()
    {
        student = new();
        studentAccounts = [];
        semesters = [];
        discountTypes = [];

            // Existing requests can reach reviewers whose selected school differs from
            // the student's school. Resolve through the request, not that UI selection.
            var requestStudentId = ProcessId is Guid processId && processId != Guid.Empty
                ? (await _uow.DiscountRequests.FindAsync(x => x.WfProcessId == processId))?.StudentId
                : null;
            student = await _uow.Students.FindAsync(x => x.Id == _model.StudentId
                    && (requestStudentId != null ? x.Id == requestStudentId : x.SchoolId == _appStateService.SchoolId),
                ["School", "CurrentContract", "CurrentContract.Status", "CurrentContract.Class", "CurrentContract.Class.Level"]);

            if (student is null)
                return;

            studentAccounts = (await _uow.StudentAccounts.FindAllAsync(x => x.ContractId == student.CurrentContractId, ["Contract", "Service", "Category", "Semester"])).ToList();

            if (student.CurrentContract is not null)
            {
                var configuredSemesters = await _uow.SchoolSemesters.FindAllAsync(x => x.SchoolId == student.SchoolId
                        && x.Year.IsCurrent && x.SemesterId > 0
                        ,["Semester"], x => x.SemesterId);

                semesters = configuredSemesters.Select(x => new BasicByteDto
                {
                    Id = x.SemesterId,
                    Name = IsArabic ? x.Semester.NameAr : x.Semester.Name,
                    NameAr = x.Semester.NameAr
                }).ToList();
            }

            await LoadDiscountTypesAsync();
    }

    private async Task OnStudentChangedAsync(object? value)
    {
        var selectedStudentId = value switch
        {
            Guid id => id,
            string text when Guid.TryParse(text, out var id) => id,
            _ => Guid.Empty
        };

        _model = new DiscountRequestDto { StudentId = selectedStudentId };
        DeletePendingAttachments();
        attachments = [];

        isLoading = true;
        try
        {
            if (_model.StudentId == Guid.Empty)
            {
                student = new();
                return;
            }

            await LoadStudentAsync();
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

    private async Task LoadDiscountTypesAsync()
    {
        discountTypes = [];
        if (student.CurrentContract is null)
            return;

        discountTypes = (await _uow.Discounts.FindAllAsync(x => x.DependencyId == 50, x => x.SN)).ToList();
    }

    private async Task OnDiscountChangedAsync(object? value)
    {
        _model.DiscountId = value switch
        {
            int id => id,
            string text when int.TryParse(text, out var id) => id,
            _ => _model.DiscountId
        };
        _model.Details = [];
        configuredDiscountAmounts = [];

        if (_model.DiscountId <= 0 || student?.CurrentContract is null)
            return;

        try
        {
            var selectedDiscount = discountTypes.FirstOrDefault(x => x.Id == _model.DiscountId);
            if (selectedDiscount is null)
                return;

            var configurations = (await _uow.DiscountDetails.FindAllAsync(x => x.DiscountId == _model.DiscountId && x.SchoolId == student.SchoolId && x.Year.IsCurrent))
                .GroupBy(x => x.SemesterId)
                .ToDictionary(x => x.Key, x => x.First());

            configurations.TryGetValue(0, out var annualConfiguration);

            var tuitionKeys = studentAccounts.Where(x => x.Categoryid == 20)
                .Select(x => (x.ContractId, x.ServiceId, x.SemesterId)).ToHashSet();
            var items = studentAccounts.Where(x => IsSettlement || (x.ContractId == student.CurrentContractId
                    && (x.Categoryid == 20 || (x.Categoryid == 90 && tuitionKeys.Contains((x.ContractId, x.ServiceId, x.SemesterId))))))
                .GroupBy(x => new { x.ContractId, x.ServiceId, x.Categoryid, x.SemesterId });
            foreach (var item in items)
            {
                var remaining = decimal.Round(item.Sum(x => (decimal)x.Debit - (decimal)x.Credit), 2);
                if (IsSettlement && remaining <= 0) continue;
                var account = item.First();
                var row = new DiscountRequestDetailDto
                {
                    ContractId = item.Key.ContractId, ServiceId = item.Key.ServiceId, CategoryId = item.Key.Categoryid,
                    SemesterId = item.Key.SemesterId,
                    SemesterName = account.Semester?.Name, SemesterNameAr = account.Semester?.NameAr,
                    ItemName = $"{account.Service?.Name} - {account.Category?.Name}",
                    ItemNameAr = $"{account.Service?.NameAr} - {account.Category?.NameAr}",
                    RemainingAmount = remaining, IsSelected = IsSettlement
                };
                configurations.TryGetValue(row.SemesterId, out var semesterConfiguration);
                var configuration = semesterConfiguration ?? annualConfiguration;
                if (!IsSettlement && configuration is not null && configuration.Amount > 0)
                {
                    var basis = item.Key.Categoryid == 90
                        ? Math.Max(0, item.Where(x => x.TransactionTypeId == Core.Enums.TransactionTypeIds.AddDues
                            || x.TransactionTypeId == Core.Enums.TransactionTypeIds.FinancialSettlement
                            || Core.Enums.TransactionTypeIds.DiscountIds.Contains(x.TransactionTypeId))
                            .Sum(x => (decimal)x.Debit - (decimal)x.Credit))
                        : StudentAccountDiscountCalculator.CalculateNetServiceFee(studentAccounts, item.Key.ContractId, item.Key.ServiceId, item.Key.SemesterId);
                    row.HasConfiguredPercentage = true;
                    row.Percentage = configuration.Amount;
                    row.Amount = (double)StudentAccountDiscountCalculator.CalculateDiscountAmount(basis, (decimal)configuration.Amount);
                    configuredDiscountAmounts[row] = row.Amount;
                }
                _model.Details.Add(row);
            }
        }
        catch (Exception ex)
        {
            _model.Details = [];
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
    }

    private void OnSemesterSelectionChanged(DiscountRequestDetailDto detail)
    {
        if (!detail.IsSelected)
        {
            detail.Amount = 0;
            SynchronizeTaxDiscounts();
            return;
        }

        if (detail.HasConfiguredPercentage
            && configuredDiscountAmounts.TryGetValue(detail, out var configuredAmount))
        {
            detail.Amount = configuredAmount;
        }
        SynchronizeTaxDiscounts();
    }

    private bool IsAutomaticTax(DiscountRequestDetailDto detail) => !IsSettlement && detail.CategoryId == 90;

    private void SynchronizeTaxDiscounts()
    {
        if (IsSettlement) return;

        // Older requests may have saved the fee without its tax row.
        foreach (var fee in _model.Details.Where(x => x.CategoryId == 20).ToList())
        {
            if (_model.Details.Any(x => x.CategoryId == 90 && x.ContractId == fee.ContractId
                && x.ServiceId == fee.ServiceId && x.SemesterId == fee.SemesterId)) continue;
            var accounts = studentAccounts.Where(x => x.Categoryid == 90 && x.ContractId == fee.ContractId
                && x.ServiceId == fee.ServiceId && x.SemesterId == fee.SemesterId).ToList();
            if (accounts.FirstOrDefault() is not { } account) continue;
            _model.Details.Add(new DiscountRequestDetailDto
            {
                ContractId = fee.ContractId, ServiceId = fee.ServiceId, CategoryId = 90,
                SemesterId = fee.SemesterId, SemesterName = fee.SemesterName, SemesterNameAr = fee.SemesterNameAr,
                ItemName = $"{account.Service?.Name} - {account.Category?.Name}",
                ItemNameAr = $"{account.Service?.NameAr} - {account.Category?.NameAr}",
                RemainingAmount = decimal.Round(accounts.Sum(x => (decimal)x.Debit - (decimal)x.Credit), 2),
                Percentage = fee.Percentage, HasConfiguredPercentage = fee.HasConfiguredPercentage
            });
        }

        foreach (var tax in _model.Details.Where(IsAutomaticTax))
        {
            var fee = _model.Details.FirstOrDefault(x => x.CategoryId == 20
                && x.ContractId == tax.ContractId && x.ServiceId == tax.ServiceId && x.SemesterId == tax.SemesterId);
            tax.IsSelected = fee?.IsSelected == true;
            tax.Amount = 0;
            if (!tax.IsSelected || fee is null || !double.IsFinite(fee.Amount) || fee.Amount <= 0
                || fee.ContractId is not Guid contractId || fee.ServiceId is not int serviceId) continue;

            var feeBasis = StudentAccountDiscountCalculator.CalculateNetServiceFee(studentAccounts, contractId, serviceId, fee.SemesterId);
            var taxBasis = Math.Max(0, studentAccounts.Where(x => x.ContractId == contractId
                    && x.ServiceId == serviceId && x.SemesterId == fee.SemesterId && x.Categoryid == 90
                    && (x.TransactionTypeId == Core.Enums.TransactionTypeIds.AddDues
                        || x.TransactionTypeId == Core.Enums.TransactionTypeIds.FinancialSettlement
                        || Core.Enums.TransactionTypeIds.DiscountIds.Contains(x.TransactionTypeId)))
                .Sum(x => (decimal)x.Debit - (decimal)x.Credit));
            if (feeBasis > 0)
                tax.Amount = (double)decimal.Round(taxBasis * (decimal)Math.Min(fee.Amount, (double)feeBasis) / feeBasis,
                    2, MidpointRounding.AwayFromZero);
        }
    }

    private async Task UploadFilesAsync(InputFileChangeEventArgs e)
    {
        foreach (var file in e.GetMultipleFiles(10))
        {
            if (!FileValidator.ValidateSize(file.Size, 25) || attachments.Sum(x => x.FileSize ?? 0) + file.Size > MaxAttachmentsSize)
            {
                _toastService.Notify(NotificationSeverity.Warning, _localizer["Attachment"], _localizer["AttachmentsSizeExceeded25MB"]);
                return;
            }

            if (!FileValidator.ValidateType(file.Name))
            {
                _toastService.Notify(NotificationSeverity.Warning, _localizer["Attachment"], _localizer["FileNotSupported"]);
                continue;
            }

            var storedName = $"W_{Path.GetRandomFileName()}{Path.GetExtension(file.Name).ToLowerInvariant()}";
            var directory = Path.Combine(_env.WebRootPath, "uploads", "workflow");
            Directory.CreateDirectory(directory);
            var fullPath = Path.Combine(directory, storedName);

            await using var source = file.OpenReadStream(MaxAttachmentsSize);
            await using var target = File.Create(fullPath);
            await source.CopyToAsync(target);

            attachments.Add(new FileDto
            {
                FileName = file.Name,
                FilePath = storedName,
                FileSize = file.Size,
                IsNew = true
            });
        }
    }

    private void RemoveAttachment(FileDto attachment)
    {
        attachments.Remove(attachment);
        if (!attachment.IsNew || string.IsNullOrWhiteSpace(attachment.FilePath))
            return;

        var path = Path.Combine(_env.WebRootPath, "uploads", "workflow", attachment.FilePath);
        if (File.Exists(path))
            File.Delete(path);
    }

    private void DeletePendingAttachments()
    {
        foreach (var attachment in attachments.Where(x => x.IsNew && !string.IsNullOrWhiteSpace(x.FilePath)))
        {
            var path = Path.Combine(_env.WebRootPath, "uploads", "workflow", attachment.FilePath!);
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private async Task SubmitAsync()
    {
        if (student is null || isSaving)
            return;

        if (_model.DiscountId == 0)
        {
            _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], _localizer["SelectDiscount"]);
            return;
        }

        if (_model.Details.Any(x => x.IsSelected && (!double.IsFinite(x.Amount) || (!IsSettlement && x.Amount < 0))))
        {
            _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], _localizer["EnterDiscountValue"]);
            return;
        }

        if (IsSettlement)
        {
            var ledger = await _uow.StudentAccounts.FindAllAsync(x => x.ContractId == student.CurrentContractId);
            if (_model.Details.Any(x => x.IsSelected && x.Amount > 0
                && x.Amount > (double)decimal.Round(ledger.Where(a => a.ContractId == x.ContractId
                    && a.ServiceId == x.ServiceId && a.Categoryid == x.CategoryId && a.SemesterId == x.SemesterId)
                    .Sum(a => (decimal)a.Debit - (decimal)a.Credit), 2)))
            {
                _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], _localizer["SettlementExceedsDueItem"]);
                return;
            }
        }

        SynchronizeTaxDiscounts();
        var selectedDetails = _model.Details.Where(x => x.IsSelected && x.Amount != 0).ToList();
        if (selectedDetails.Count == 0)
        {
            _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], _localizer["EnterDiscountValue"]);
            return;
        }

        try
        {
            isSaving = true;
            if (_model.Id == Guid.Empty)
            {
                var wf = await _uow.Workflows.FindAsync(x => x.TypeId == 1 && x.IsActive);
                if (wf == null)
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["NoWF_FoundForThisType"]);
                    return;
                }
                var wfTasks = await _uow.WorkflowTasks.FindAllAsync(x => x.WorkflowId == wf.Id);
                if (wfTasks.Count() == 0)
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["NoWF_FoundForThisType"]);
                    return;
                }

                var wfProcess = new WorkflowProcess()
                {
                    WorkflowId = wf.Id,
                    StatusId = 2,
                    RefNo = await WfProcessIdGenerator.GetProcessId(_uow),
                    RequestedForId = _model.StudentId,
                    CreatedBy = await _userService.GetUserIdAsync(),
                    CreatedOn = DateTime.UtcNow.GetKsaDateTime()
                };
                var history = new WorkflowHistory()
                {
                    ActionId = 1,
                    TaskId = wfTasks.First().Id,
                    Comments = _model.Comments.Trim(),
                    CreatorId = wfProcess.CreatedBy,
                    CreatedOn = wfProcess.CreatedOn
                };
                foreach (var file in attachments)
                {
                    history.Attachments.Add(new()
                    {
                        FilePath = file.FilePath,
                        FileName = file.FileName,
                    });
                }
                wfProcess.WorkflowHistories.Add(history);

                foreach (var task in wfTasks)
                {
                    var processTask = new WorkflowProcessTask()
                    {
                        StatusId = 1,
                        WfTaskId = task.Id
                    };
                    if (task.SN == 0 && task.IsTheEnd)
                    {
                        processTask.StartDate = processTask.EndDate = DateTime.UtcNow.GetKsaDateTime();
                        processTask.StatusId = 10;
                        wfProcess.StatusId = 10;
                    }
                    else if (task.SN == 0)
                    {
                        processTask.StartDate = processTask.EndDate = DateTime.UtcNow.GetKsaDateTime();
                        processTask.StatusId = 10;
                    }
                    else if (task.SN == 1)
                    {
                        processTask.StartDate = DateTime.UtcNow.GetKsaDateTime();
                        processTask.EndDate = DateTime.UtcNow.GetKsaDateTime().AddDays(task.RequiredDays);
                        processTask.StatusId = 2;
                    }
                    wfProcess.WorkflowProcessTasks.Add(processTask);
                }

                // Map the comming data to the correct format

                var request = _mapper.Map<DiscountRequest>(_model);
                request.CreatedBy = wfProcess.CreatedBy;
                request.CreatedOn = wfProcess.CreatedOn;
                request.Details = _mapper.Map<List<DiscountRequestDetail>>(selectedDetails);

                wfProcess.DiscountRequests.Add(request);
                await _uow.WorkflowProcesses.AddAsync(wfProcess);

                if (await _uow.SaveAsync())
                {
                    _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                    _navigator.NavigateTo("inbox");
                }
                else
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
            }
            else
            {
                var request = await _uow.DiscountRequests.FindAsync(x => x.Id == _model.Id, new[] { "Details", "WfProcess", "WfProcess.WorkflowProcessTasks" });
                if (request is null)
                {
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                    return;
                }
                var currentTask = request.WfProcess.WorkflowProcessTasks.FirstOrDefault(x => x.StatusId == 2);
                if (currentTask is null)
                    currentTask = request.WfProcess.WorkflowProcessTasks.OrderByDescending(x => x.StartDate).FirstOrDefault();
                var history = new WorkflowHistory()
                {
                    WfProcessId = request.WfProcessId,
                    ActionId = 10,
                    TaskId = currentTask != null ? currentTask.WfTaskId : 0,
                    Comments = _model.Comments.Trim(),
                    CreatorId = await _userService.GetUserIdAsync(),
                    CreatedOn = DateTime.UtcNow.GetKsaDateTime()
                };

                foreach (var file in _model.Attachments.Where(x => x.IsNew))
                {
                    history.Attachments.Add(new()
                    {
                        FilePath = file.FilePath,
                        FileName = file.FileName,
                    });
                }
                await _uow.WorkflowHistories.AddAsync(history);

                // Reads are detached: replacing Details alone would leave the old rows
                // in the database. Delete scalar-only stubs to avoid attaching the old
                // request graph, then save the replacements in the same transaction.
                _uow.DiscountRequestDetails.DeleteRange(request.Details.Select(x => new DiscountRequestDetail
                {
                    Id = x.Id,
                    RequestId = request.Id
                }));
                _mapper.Map(_model, request);
                request.UpdatedBy = await _userService.GetUserIdAsync();
                request.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();
                request.Details = _mapper.Map<List<DiscountRequestDetail>>(selectedDetails);
                await _uow.DiscountRequests.Update(request);

                if (await _uow.SaveAsync())
                {
                    _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                }
                else
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
            }
            attachments = [];
        }
        catch (Exception ex)
        {
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
        finally
        {
            isSaving = false;
        }
    }

    private string SemesterName(DiscountRequestDetailDto detail) => IsArabic ? detail.SemesterNameAr ?? detail.SemesterName ?? "-" : detail.SemesterName ?? "-";
}
