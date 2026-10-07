namespace Web.Components.Pages.Reports;

public partial class BriefAccount
{
    private static readonly HashSet<int> DiscountTransactionTypes = TransactionTypeIds.DiscountIds;
    private readonly StudentAccountReportExcelExporter excelExporter;
    private School? school;
    private List<BriefAccountReportExportRow> rows = [];
    private bool isLoading;
    private bool isExporting;
    private bool reportFound;
    private DateTime printedOn;

    public BriefAccount(StudentAccountReportExcelExporter excelExporter)
    {
        this.excelExporter = excelExporter;
    }

    [Parameter] public string Scope { get; set; } = string.Empty;
    [Parameter] public Guid Id { get; set; }

    private bool isArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
    private string SchoolName => school is null ? string.Empty : Localized(school.Name, school.NameAr);
    private IReadOnlyList<double> Totals =>
    [
        rows.Sum(x => x.PreviousBalance), rows.Sum(x => x.PreviousBalanceAdjustment), rows.Sum(x => x.Dues),
        rows.Sum(x => x.DuesWithdrawal), rows.Sum(x => x.Discounts), rows.Sum(x => x.Tax),
        rows.Sum(x => x.NetDue), rows.Sum(x => x.Paid), rows.Sum(x => x.Remaining)
    ];

    protected override async Task OnParametersSetAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        reportFound = false;
        rows = [];
        try
        {
            await _appStateService.InitializeAsync();
            if (_appStateService.SchoolId == Guid.Empty) return;

            school = await _uow.Schools.FindAsync(x => x.Id == _appStateService.SchoolId);
            if (school is null) return;

            var normalizedScope = Scope.Trim().ToLowerInvariant();
            List<Student> students;
            if (normalizedScope == "student")
            {
                var selectedStudent = await _uow.Students.FindAsync(
                    x => x.Id == Id && x.SchoolId == _appStateService.SchoolId,
                    ["Parent", "CurrentContract", "CurrentContract.Class", "CurrentContract.Class.Level", "CurrentContract.Status"]);
                if (selectedStudent is null) return;
                students = [selectedStudent];
                reportFound = true;
            }
            else if (normalizedScope == "parent")
            {
                var parentExists = await _uow.Parents.Existing(x => x.Id == Id);
                if (!parentExists) return;
                students = (await _uow.Students.FindAllAsync(
                    x => x.ParentId == Id && x.SchoolId == _appStateService.SchoolId,
                    ["Parent", "CurrentContract", "CurrentContract.Class", "CurrentContract.Class.Level", "CurrentContract.Status"]))
                    .OrderBy(x => x.SN)
                    .ToList();
                reportFound = true;
            }
            else
            {
                return;
            }

            var currentContractIds = students
                .Where(x => x.CurrentContractId != Guid.Empty)
                .Select(x => x.CurrentContractId)
                .ToArray();
            var accounts = currentContractIds.Length == 0
                ? []
                : (await _uow.StudentAccounts.FindAllAsync(
                    x => currentContractIds.Contains(x.ContractId)
                        && x.Contract.Year.IsCurrent,
                    ["Contract", "Contract.Year"])).ToList();
            var accountsByStudent = accounts
                .GroupBy(x => x.Contract.StudentId)
                .ToDictionary(x => x.Key, x => x.ToList());

            rows = students.Select((student, index) =>
            {
                var studentAccounts = accountsByStudent.GetValueOrDefault(student.Id) ?? [];
                var previousBalance = Net(studentAccounts.Where(x => x.Categoryid == 10 && x.TransactionTypeId != TransactionTypeIds.FinancialSettlement));
                var previousAdjustment = Net(studentAccounts.Where(x => x.Categoryid == 10 && x.TransactionTypeId == TransactionTypeIds.FinancialSettlement));
                
                var dues = Net(studentAccounts.Where(x =>
                    (x.TransactionTypeId == TransactionTypeIds.AddDues || x.TransactionTypeId == TransactionTypeIds.WithdrawalPenalty)
                    && x.Categoryid is not (10 or 90)));

                var duesWithdrawal = CreditNet(studentAccounts.Where(x => x.TransactionTypeId == TransactionTypeIds.DuesWithdrawal));
                var discounts = CreditNet(studentAccounts.Where(x => DiscountTransactionTypes.Contains(x.TransactionTypeId)));
                var tax = Net(studentAccounts.Where(x => x.Categoryid == 90 && x.TransactionTypeId == TransactionTypeIds.DiscountWithdrawal));
                var paid = CreditNet(studentAccounts.Where(x =>
                    x.TransactionTypeId == TransactionTypeIds.DuesPayment || x.TransactionTypeId == TransactionTypeIds.FeeRefund));
                // The adjustment is reported for transparency but is already included
                // in the carried previous balance, so it must not be counted twice.
                var netDue = previousBalance + dues - duesWithdrawal - discounts + tax;
                var remaining = netDue - paid;
                var currentContract = student.CurrentContract;
                var level = currentContract?.Class is null
                    ? string.Empty
                    : $"{Localized(currentContract.Class.Level.Name, currentContract.Class.Level.NameAr)} - {Localized(currentContract.Class.Name, currentContract.Class.NameAr)}";
                var status = currentContract?.Status is null
                    ? string.Empty
                    : Localized(currentContract.Status.Name, currentContract.Status.NameAr);

                return new BriefAccountReportExportRow(
                    index + 1,
                    $"{school.Code}-{student.SN:D5}",
                    $"{student.Name} {student.FatherName}".Trim(),
                    level,
                    status,
                    student.Parent?.Mobile ?? string.Empty,
                    previousBalance,
                    previousAdjustment,
                    dues,
                    duesWithdrawal,
                    discounts,
                    tax,
                    netDue,
                    paid,
                    remaining);
            }).ToList();

            printedOn = DateTime.UtcNow.GetKsaDateTime();
        }
        catch (Exception ex)
        {
            reportFound = false;
            rows = [];
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    private static double Net(IEnumerable<StudentAccount> accounts) => accounts.Sum(x => x.Debit - x.Credit);
    private static double CreditNet(IEnumerable<StudentAccount> accounts) => accounts.Sum(x => x.Credit - x.Debit);
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
        await _js.InvokeVoidAsync("APP.printReport", $"Brief-account-{Scope}-{Id}");
    }

    private async Task ExportExcelAsync()
    {
        if (!reportFound || school is null || isExporting) return;
        isExporting = true;
        try
        {
            printedOn = DateTime.UtcNow.GetKsaDateTime();
            var headers = new[]
            {
                "#", _localizer["StudentNumber"].Value, _localizer["StudentName"].Value,
                _localizer["SchoolLevel"].Value, _localizer["StudentStatus"].Value, _localizer["ParentMobile"].Value,
                _localizer["PreviousBalance"].Value, _localizer["PreviousBalanceAdjustment"].Value,
                _localizer["Dues"].Value, _localizer["DuesWithdrawal"].Value, _localizer["Discounts"].Value,
                _localizer["Tax"].Value, _localizer["NetDue"].Value, _localizer["Paid"].Value,
                _localizer["Remaining"].Value
            };
            var report = new BriefAccountReportExport(
                _localizer["BriefAccount"], _localizer["BriefAccount"], SchoolName,
                _localizer["PrintDate"], printedOn, isArabic, headers, rows,
                _localizer["Total"], Totals);
            var bytes = excelExporter.Export(report);
            await _js.InvokeVoidAsync("saveAsFile", $"Brief-account-{Scope}-{Id}.xlsx", Convert.ToBase64String(bytes));
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
