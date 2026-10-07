namespace Web.Components.Pages.Reports;

public partial class DetailedAccount
{
    private readonly StudentAccountReportExcelExporter excelExporter;
    private Student? student;
    private List<DetailedAccountGroupExport> groups = [];
    private bool isLoading;
    private bool isExporting;
    private DateTime printedOn;

    public DetailedAccount(StudentAccountReportExcelExporter excelExporter)
    {
        this.excelExporter = excelExporter;
    }

    [Parameter] public Guid StudentId { get; set; }

    private bool isArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
    private string StudentCode => student is null ? string.Empty : $"{student.School.Code}-{student.SN:D5}";
    private string StudentFullName => student is null ? string.Empty : $"{student.Name} {student.FatherName}".Trim();
    private string SchoolName => student is null ? string.Empty : Localized(student.School.Name, student.School.NameAr);
    private double TotalDebit => groups.Sum(x => x.TotalDebit);
    private double TotalCredit => groups.Sum(x => x.TotalCredit);
    private double Remaining => TotalDebit - TotalCredit;

    protected override async Task OnParametersSetAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        try
        {
            await _appStateService.InitializeAsync();
            student = await _uow.Students.FindAsync(x => x.Id == StudentId && x.SchoolId == _appStateService.SchoolId, ["School"]);

            if (student is null)
            {
                groups = [];
                return;
            }

            var accounts = await _uow.StudentAccounts.FindAllAsync( x => x.ContractId == student.CurrentContractId
                    && x.Contract.StudentId == StudentId && x.Contract.Year.IsCurrent
                    && x.Contract.Student.SchoolId == _appStateService.SchoolId,
                ["Contract", "Contract.Year", "Semester", "Service", "Category", "TransactionType"]);

            var number = 1;
            groups = accounts
                .OrderBy(x => x.SemesterId)
                .ThenBy(x=> x.TransactionTypeId)
                .ThenBy(x => x.Date)
                .GroupBy(x => new
                {
                    x.Categoryid,
                    Name = Localized(x.Category.Name, x.Category.NameAr)
                })
                .Select(group =>
                {
                    var rows = group.Select(account => new DetailedAccountReportExportRow(
                        number++,
                        Localized(account.Service.Name, account.Service.NameAr),
                        Localized(account.Semester.Name, account.Semester.NameAr),
                        Localized(account.TransactionType.Name, account.TransactionType.NameAr),
                        account.Date,
                        account.Debit,
                        account.Credit,
                        account.Debit - account.Credit)).ToList();

                    return new DetailedAccountGroupExport(group.Key.Name, rows, rows.Sum(x => x.Debit), rows.Sum(x => x.Credit), rows.Sum(x => x.Remaining));
                }).ToList();

            printedOn = DateTime.UtcNow.GetKsaDateTime();
        }
        catch (Exception ex)
        {
            student = null;
            groups = [];
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    private string Localized(string english, string arabic) => isArabic ? arabic : english;

    private async Task PrintAsync()
    {
        printedOn = DateTime.UtcNow.GetKsaDateTime();
        await InvokeAsync(StateHasChanged);
        await _js.InvokeVoidAsync("APP.printReport");
    }

    private async Task ExportPdfAsync()
    {
        printedOn = DateTime.UtcNow.GetKsaDateTime();
        await InvokeAsync(StateHasChanged);
        await _js.InvokeVoidAsync("APP.printReport", $"Detailed-account-{StudentCode}");
    }

    private async Task ExportExcelAsync()
    {
        if (student is null || isExporting) return;
        isExporting = true;
        try
        {
            printedOn = DateTime.UtcNow.GetKsaDateTime();
            var headers = new[]
            {
                "#", _localizer["Service"].Value, _localizer["Semester"].Value,
                _localizer["Operation"].Value, _localizer["TransactionDate"].Value,
                _localizer["Debit"].Value, _localizer["Credit"].Value, _localizer["Remaining"].Value
            };
            var report = new DetailedAccountReportExport(
                _localizer["DetailedAccountStatement"], _localizer["DetailedAccountStatement"],
                SchoolName, _localizer["StudentName"], StudentFullName,
                _localizer["PrintDate"], printedOn, isArabic, headers, groups,
                _localizer["Total"], _localizer["AccountSummary"],
                TotalDebit, TotalCredit, Remaining);
            var bytes = excelExporter.Export(report);
            await _js.InvokeVoidAsync("saveAsFile", $"Detailed-account-{StudentCode}.xlsx", Convert.ToBase64String(bytes));
        }
        catch (Exception ex)
        {
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
        finally
        {
            isExporting = false;
        }
    }
}
