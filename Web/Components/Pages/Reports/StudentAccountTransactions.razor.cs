namespace Web.Components.Pages.Reports;

public partial class StudentAccountTransactions
{
    private readonly StudentAccountReportExcelExporter excelExporter;
    private Student? student;
    private List<StudentAccountReportExportRow> rows = [];
    private bool isLoading;
    private bool isExporting;
    private DateTime printedOn;

    public StudentAccountTransactions(StudentAccountReportExcelExporter excelExporter)
    {
        this.excelExporter = excelExporter;
    }

    [Parameter]
    public Guid StudentId { get; set; }

    private bool isArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
    private string StudentCode => student is null ? string.Empty : $"{student.School.Code}-{student.SN:D5}";
    private string StudentFullName => student is null ? string.Empty : $"{student.Name} {student.FatherName}".Trim();
    private string SchoolName => student is null ? string.Empty : Localized(student.School.Name, student.School.NameAr);
    private double TotalDebit => rows.Sum(x => x.Debit);
    private double TotalCredit => rows.Sum(x => x.Credit);
    private double FinalBalance => rows.LastOrDefault()?.Balance ?? 0;

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
                rows = [];
                return;
            }

            var accounts = await _uow.StudentAccounts.FindAllAsync(x => x.ContractId == student.CurrentContractId
                    && x.Contract.StudentId == StudentId && x.Contract.Year.IsCurrent
                    && x.Contract.Student.SchoolId == _appStateService.SchoolId
                ,["Contract", "Contract.Year", "Contract.Class", "Contract.Class.Level", "Semester", "Service", "Category"]);

            var ordered = accounts
                .OrderBy(x => x.SemesterId)
                .ThenBy(x => x.TransactionTypeId)
                .ThenBy(x=> x.Categoryid)
                .ThenBy(x=> x.ServiceId)
                .ThenBy(x => x.Date)
                .ToList();
            var balance = 0d;
            rows = ordered.Select((account, index) =>
            {
                balance += account.Debit - account.Credit;
                return new StudentAccountReportExportRow(
                    index + 1,
                    Localized(account.Category.Name, account.Category.NameAr),
                    Localized(account.Service.Name, account.Service.NameAr),
                    Localized(account.Semester.Name, account.Semester.NameAr),
                    account.Comments ?? "",
                    Localized(account.Contract.Class.Level.Name, account.Contract.Class.Level.NameAr),
                    Localized(account.Contract.Class.Name, account.Contract.Class.NameAr),
                    account.Date,
                    account.Debit,
                    account.Credit,
                    balance,
                    account.CreatedOn);
            }).ToList();

            printedOn = DateTime.UtcNow.GetKsaDateTime();
        }
        catch (Exception ex)
        {
            student = null;
            rows = [];
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
        await _js.InvokeVoidAsync("APP.printReport", $"Student-account-{StudentCode}");
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
                "#", _localizer["Item"].Value, _localizer["Service"].Value, _localizer["Semester"].Value,
                _localizer["Description"].Value, _localizer["SchoolLevel"].Value, _localizer["Class"].Value,
                _localizer["TransactionDate"].Value, _localizer["Debit"].Value, _localizer["Credit"].Value,
                _localizer["Balance"].Value, _localizer["AdditionDate"].Value
            };

            var report = new StudentAccountReportExport(
                _localizer["StudentAccountTransactions"],
                _localizer["StudentAccountTransactions"],
                StudentCode,
                StudentFullName,
                _localizer["PrintDate"],
                printedOn,
                isArabic,
                headers,
                rows,
                _localizer["Total"],
                TotalDebit,
                TotalCredit,
                FinalBalance);

            var bytes = excelExporter.Export(report);
            await _js.InvokeVoidAsync("saveAsFile", $"Student-account-{StudentCode}.xlsx", Convert.ToBase64String(bytes));
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
