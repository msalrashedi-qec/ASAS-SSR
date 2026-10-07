
using Core.Entities.General;

namespace Web.Components.Pages.Reports;

public partial class Reports
{
    private static readonly HashSet<int> DiscountTransactionTypes = TransactionTypeIds.DiscountIds;
    private readonly StudentAccountReportExcelExporter excelExporter;

    private Report ActiveReport { get; set; } = new();
    private School? school;
    private List<Student> allStudents = [];
    private List<FilterOption<int>> yearOptions = [];
    private List<FilterOption<int>> levelOptions = [];
    private List<FilterOption<int>> classOptions = [];
    private List<FilterOption<byte>> statusOptions = [];
    private List<FilterOption<Guid>> studentOptions = [];
    private List<FilterOption<int>> serviceOptions = [];
    private List<FilterOption<byte>> semesterOptions = [];
    private List<FilterOption<byte>> paymentMethodOptions = [];
    private readonly List<FilterOption<string>> voucherTypeOptions = [new("all", "الكل"), new("receipt", "سندات القبض"), new("refund", "سندات الصرف")];
    private List<FilterOption<int>> transactionTypeOptions = [];
    private List<FilterOption<Guid>> classRoomOptions = [];
    private List<FilterOption<int>> busOptions = [];
    private readonly List<FilterOption<string>> allocationOptions = [new("All", "الكل"), new("Assigned", "المسكنين"), new("Unassigned", "غير المسكنين")];
    private List<SortOption> sortOptions = [];

    private int? selectedYearId;
    private int? currentYearId;
    private IEnumerable<int> selectedLevelIds = [];
    private IEnumerable<int> selectedClassIds = [];
    private IEnumerable<byte> selectedStatusIds = [];
    private IEnumerable<Guid> selectedStudentIds = [];
    private IEnumerable<int> selectedServiceIds = [];
    private IEnumerable<byte> selectedSemesterIds = [];
    private IEnumerable<byte> selectedPaymentMethodIds = [];
    private string selectedVoucherType = "all";
    private IEnumerable<int> selectedTransactionTypeIds = [];
    private IEnumerable<Guid> selectedClassRoomIds = [];
    private IEnumerable<int> selectedBusIds = [];
    private string allocationMode = "all";
    private string selectedSortKey = "number";
    private bool sortDescending;
    private DateTime? dateFrom;
    private DateTime? dateTo;

    private string activeReportKey = "students";
    private List<string> headers = [];
    private List<GenericReportExportRow> rows = [];
    private List<SummaryCard> summaryCards = [];
    private string filterSummary = "كل البيانات المتاحة";
    private bool isLoading;
    private bool isExporting;
    private bool hasRun;
    private DateTime printedOn = DateTime.UtcNow.GetKsaDateTime();

    public Reports(StudentAccountReportExcelExporter excelExporter)
    {
        this.excelExporter = excelExporter;
    }

    [Parameter] public string ReportKey { get; set; } = "students";

    private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
    private string SchoolName => school is null ? string.Empty : Localized(school.Name, school.NameAr);
    private string ActiveReportTitle => Localized(ActiveReport.Name, ActiveReport.NameAr);
    private bool ShowAcademicFilters => activeReportKey is "students" or "financial-detail" or "financial-summary" or "service-payments" or "classrooms" or "service-prices";
    private bool ShowStatusFilter => !IsMonthlyReport && activeReportKey is not ("receipts" or "account-transactions" or "discounts" or "service-prices");
    private bool ShowStudentFilter => !IsMonthlyReport && activeReportKey != "service-prices";
    private bool ShowServiceFilter => activeReportKey is "financial-detail" or "account-transactions" or "discounts" or "service-payments" or "service-prices";
    private bool ShowSemesterFilter => ShowServiceFilter;
    private bool ShowPaymentFilter => activeReportKey == "receipts";
    private bool ShowVoucherTypeFilter => activeReportKey == "receipts";
    private bool ShowTransactionFilter => activeReportKey is "account-transactions" or "discounts";
    private bool ShowClassroomFilter => activeReportKey == "classrooms";
    private bool ShowBusFilter => activeReportKey == "buses";
    private bool ShowAllocationFilter => activeReportKey is "classrooms" or "buses";
    private bool ShowDateFilter => activeReportKey is "students" or "account-transactions" or "receipts" or "discounts" or "service-payments";
    private bool IsUltraWideReport => activeReportKey == "financial-detail";

    protected override async Task OnParametersSetAsync()
    {
        var leavingMonthlyReport = IsMonthlyReport && ReportKey != "monthly-students";
        hasRun = false;
        var userId = await _userService.GetUserIdAsync();
        var roleIds = (await _uow.UserRoles.FindAllAsync(x => x.UserId == userId))
            .Select(x => x.RoleId)
            .ToList();
        var roleReport = roleIds.Count == 0
            ? null
            : await _uow.RoleReports.FindAsync(
                x => roleIds.Contains(x.RoleId) && x.Report.Url == (ReportKey == "monthly-students" ? "students" : ReportKey),
                ["Report"]);

        if (roleReport is null)
        {
            _navigator.NavigateTo("/Account/AccessDenied", replace: true);
            return;
        }

        ActiveReport = ReportKey == "monthly-students"
            ? new Report { Url = "monthly-students", Name = "Monthly student statistics", NameAr = "إحصائية أعداد الطلاب في كل شهر" }
            : roleReport.Report;
        activeReportKey = ActiveReport.Url;
        ConfigureSortOptions();
        if (IsMonthlyReport)
        {
            await LoadMonthlyReferencesAsync(userId);
            return;
        }
        if (school is null || leavingMonthlyReport) await LoadReferenceDataAsync();
        if (school is not null) await RunReportAsync();
    }

    private async Task LoadReferenceDataAsync()
    {
        isLoading = true;
        try
        {
            await _appStateService.InitializeAsync();
            if (_appStateService.SchoolId == Guid.Empty) return;

            school = await _uow.Schools.FindAsync(x => x.Id == _appStateService.SchoolId);
            allStudents = (await _uow.Students.FindAllAsync(x => x.SchoolId == _appStateService.SchoolId
            ,["Parent", "Nationality", "Gender", "CurrentContract", "CurrentContract.Year", "CurrentContract.Class", "CurrentContract.Class.Level", "CurrentContract.Status"]))
                .OrderBy(x => x.SN).ToList();

            var years = (await _uow.Years.GetAllAsync()).ToList();
            yearOptions = years.OrderBy(x => x.Name).Select(x => new FilterOption<int>(x.Id, x.Name)).ToList();
            currentYearId = years.FirstOrDefault(x => x.IsCurrent)?.Id;
            selectedYearId = currentYearId;
            levelOptions = allStudents.Where(x => x.CurrentContract?.Class?.Level is not null)
                .GroupBy(x => x.CurrentContract.Class.LevelId).Select(x => new FilterOption<int>(x.Key, Localized(x.First().CurrentContract.Class.Level.Name, x.First().CurrentContract.Class.Level.NameAr))).OrderBy(x => x.Name).ToList();
            classOptions = allStudents.Where(x => x.CurrentContract?.Class is not null)
                .GroupBy(x => x.CurrentContract.ClassId).Select(x => new FilterOption<int>(x.Key, Localized(x.First().CurrentContract.Class.Name, x.First().CurrentContract.Class.NameAr))).OrderBy(x => x.Name).ToList();
            statusOptions = allStudents.Where(x => x.CurrentContract?.Status is not null)
                .GroupBy(x => x.CurrentContract.StatusId).Select(x => new FilterOption<byte>(x.Key, Localized(x.First().CurrentContract.Status.Name, x.First().CurrentContract.Status.NameAr))).OrderBy(x => x.Name).ToList();
            studentOptions = allStudents.Select(x => new FilterOption<Guid>(x.Id, $"{StudentCode(x)} - {StudentName(x)}")).ToList();

            serviceOptions = (await _uow.Services.FindAllAsync(x =>
                    x.SchoolId == _appStateService.SchoolId || x.SchoolId == Guid.Empty))
                .OrderBy(x => Localized(x.Name, x.NameAr)).Select(x => new FilterOption<int>(x.Id, Localized(x.Name, x.NameAr))).ToList();
            semesterOptions = (await _uow.Semesters.GetAllAsync()).OrderBy(x => x.Id)
                .Select(x => new FilterOption<byte>(x.Id, Localized(x.Name, x.NameAr))).ToList();
            paymentMethodOptions = (await _uow.PaymentMethods.GetAllAsync()).OrderBy(x => x.Id)
                .Select(x => new FilterOption<byte>(x.Id, Localized(x.Name, x.NameAr))).ToList();
            transactionTypeOptions = (await _uow.TransactionTypes.GetAllAsync()).OrderBy(x => x.Id)
                .Select(x => new FilterOption<int>(x.Id, Localized(x.Name, x.NameAr))).ToList();
            var activeClassIds = allStudents.Where(x => x.CurrentContract is not null)
                .Select(x => x.CurrentContract.ClassId).Distinct().ToArray();
            classRoomOptions = (await _uow.SchoolClassRooms.FindAllAsync(x => activeClassIds.Contains(x.ClassId), ["Class", "Class.Level"]))
                .OrderBy(x => x.Class.LevelId).ThenBy(x => x.ClassId).ThenBy(x => x.Name)
                .Select(x => new FilterOption<Guid>(x.Id, $"{Localized(x.Class.Level.Name, x.Class.Level.NameAr)} - {Localized(x.Class.Name, x.Class.NameAr)} - {Localized(x.Name, x.NameAr)}")).ToList();
            busOptions = (await _uow.Buses.FindAllAsync(x => x.SchoolId == _appStateService.SchoolId)).OrderBy(x => x.Name)
                .Select(x => new FilterOption<int>(x.Id, $"{x.Name} - {x.PlateNo}")).ToList();
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

    private async Task ResetFiltersAsync()
    {
        selectedYearId = currentYearId;
        selectedLevelIds = [];
        selectedClassIds = [];
        selectedStatusIds = [];
        selectedStudentIds = [];
        selectedServiceIds = [];
        selectedSemesterIds = [];
        selectedPaymentMethodIds = [];
        selectedVoucherType = "all";
        selectedTransactionTypeIds = [];
        selectedClassRoomIds = [];
        selectedBusIds = [];
        allocationMode = "all";
        sortDescending = false;
        ConfigureSortOptions();
        dateFrom = null;
        dateTo = null;
        await RunReportAsync();
    }

    private async Task RunReportAsync()
    {
        if (IsMonthlyReport) { await RunMonthlyReportAsync(); return; }
        if (school is null) return;
        if (!selectedYearId.HasValue || !yearOptions.Any(x => x.Id == selectedYearId.Value))
        {
            hasRun = false;
            rows = [];
            headers = [];
            summaryCards = [];
            _toastService.Notify(NotificationSeverity.Warning, "تنبيه", "يجب اختيار السنة الدراسية لعرض التقرير.");
            return;
        }
        if (dateFrom.HasValue && dateTo.HasValue && dateFrom.Value.Date > dateTo.Value.Date)
        {
            _toastService.Notify(NotificationSeverity.Warning, "تنبيه", "تاريخ البداية يجب أن يسبق تاريخ النهاية.");
            return;
        }

        isLoading = true;
        hasRun = false;
        rows = [];
        headers = [];
        summaryCards = [];
        try
        {
            var students = FilterStudents();
            var studentIds = students.Select(x => x.Id).ToHashSet();
            switch (activeReportKey)
            {
                case "students": BuildStudentReport(students); break;
                case "financial-detail": await BuildFinancialDetailAsync(students, studentIds); break;
                case "financial-summary": await BuildFinancialSummaryAsync(students, studentIds); break;
                case "account-transactions": await BuildAccountTransactionsAsync(studentIds); break;
                case "receipts": await BuildReceiptsAsync(studentIds); break;
                case "discounts": await BuildDiscountsAsync(studentIds); break;
                case "service-payments": await BuildServicePaymentsAsync(studentIds); break;
                case "classrooms": await BuildClassroomsAsync(studentIds); break;
                case "buses": await BuildBusesAsync(studentIds); break;
                case "service-prices": await BuildServicePricesAsync(); break;
            }

            ApplySort();

            printedOn = DateTime.UtcNow.GetKsaDateTime();
            filterSummary = BuildFilterSummary();
            hasRun = true;
        }
        catch (Exception ex)
        {
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
            hasRun = true;
        }
        finally
        {
            isLoading = false;
        }
    }

    private List<Student> FilterStudents()
    {
        var levels = selectedLevelIds.ToHashSet();
        var classes = selectedClassIds.ToHashSet();
        var statuses = selectedStatusIds.ToHashSet();
        var students = selectedStudentIds.ToHashSet();
        return allStudents.Where(x =>
            x.CurrentContract != null && x.CurrentContract.YearId == selectedYearId &&
            (levels.Count == 0 || levels.Contains(x.CurrentContract.Class.LevelId)) &&
            (classes.Count == 0 || classes.Contains(x.CurrentContract.ClassId)) &&
            (statuses.Count == 0 || statuses.Contains(x.CurrentContract.StatusId)) &&
            (students.Count == 0 || students.Contains(x.Id)) &&
            (activeReportKey != "students" || DateMatches(x.CurrentContract.StartDate))).ToList();
    }

    private void BuildStudentReport(List<Student> students)
    {
        headers = ["#", "رقم الطالب", "اسم الطالب", "الجنس", "المرحلة", "الصف", "الحالة", "رقم الهوية", "الجنسية", "جوال ولي الأمر", "رقم هوية ولي الأمر", "أول انضمام", "تاريخ الانسحاب", "إعادة التسجيل"];
        rows = students.Select((x, index) => Row(
            Num(index + 1), Text(StudentCode(x)), Text(StudentName(x)), Text(Localized(x.Gender.Name, x.Gender.NameAr)),
            Text(LevelName(x)), Text(ClassName(x)), Text(StatusName(x)), Text(x.NationalID),
            Text(Localized(x.Nationality.Name, x.Nationality.NameAr)), Text(x.Parent?.Mobile), Text(x.Parent?.NationalID),
            Text(ReportFormatter.Date(x.CurrentContract.StartDate)), Text(x.CurrentContract.ActualEndDate.HasValue ? ReportFormatter.Date(x.CurrentContract.ActualEndDate.Value) : string.Empty),
            Text(x.CurrentContract.RegisteredForYearId != x.CurrentContract.YearId ? ReportFormatter.Date(x.CurrentContract.CreatedOn) : string.Empty))).ToList();
        summaryCards = [new("عدد الطلاب", rows.Count.ToString("N0"), "groups"), new("أولياء الأمور", students.Select(x => x.ParentId).Distinct().Count().ToString("N0"), "family_restroom")];
    }

    private async Task BuildFinancialDetailAsync(List<Student> students, HashSet<Guid> studentIds)
    {
        var accounts = await GetFilteredAccountsAsync(studentIds, applyDateRange: false);
        var categories = accounts.Where(IsDue).GroupBy(x => new { x.Categoryid, Name = Localized(x.Category.Name, x.Category.NameAr) })
            .Select(x => (x.Key.Categoryid, x.Key.Name)).OrderBy(x => x.Name).Take(7).ToList();
        headers = ["#", "رقم الطالب", "اسم الطالب", "المرحلة", "الصف", "الحالة", "جوال ولي الأمر", "الرصيد السابق"];
        headers.AddRange(categories.Select(x => x.Name));
        headers.AddRange(["إجمالي المستحقات", "المسحوب", "الخصومات", "الضريبة", "صافي المستحق", "المسدد", "المردود", "المتبقي"]);

        foreach (var (student, index) in students.Select((value, index) => (value, index)))
        {
            var items = accounts.Where(x => x.Contract.StudentId == student.Id).ToList();
            var previous = Net(items.Where(x => x.Categoryid == 10 && x.TransactionTypeId != TransactionTypeIds.FinancialSettlement));
            var duesByCategory = categories.Select(c => Net(items.Where(x => IsDue(x) && x.Categoryid == c.Categoryid))).ToList();
            var dues = duesByCategory.Sum();
            var withdrawn = CreditNet(items.Where(x => x.TransactionTypeId == TransactionTypeIds.DuesWithdrawal));
            var discounts = CreditNet(items.Where(x => DiscountTransactionTypes.Contains(x.TransactionTypeId)));
            var tax = Net(items.Where(x => x.Categoryid == 90 && x.TransactionTypeId == TransactionTypeIds.AddDues));
            var paid = CreditNet(items.Where(x => x.TransactionTypeId == TransactionTypeIds.DuesPayment));
            var refunded = Net(items.Where(x => x.TransactionTypeId == TransactionTypeIds.FeeRefund));
            var netDue = previous + dues - withdrawn - discounts + tax;
            var remaining = netDue - paid + refunded;
            var cells = new List<GenericReportCell> { Num(index + 1), Text(StudentCode(student)), Text(StudentName(student)), Text(LevelName(student)), Text(ClassName(student)), Text(StatusName(student)), Text(student.Parent?.Mobile), Money(previous) };
            cells.AddRange(duesByCategory.Select(Money));
            cells.AddRange([Money(dues), Money(withdrawn), Money(discounts), Money(tax), Money(netDue), Money(paid), Money(refunded), Money(remaining)]);
            rows.Add(new GenericReportExportRow(cells));
        }

        AddFinancialSummary(rows, headers.IndexOf("إجمالي المستحقات"), headers.IndexOf("المسدد"), headers.IndexOf("المتبقي"));
    }

    private async Task BuildFinancialSummaryAsync(List<Student> students, HashSet<Guid> studentIds)
    {
        var accounts = await GetFilteredAccountsAsync(studentIds, applyDateRange: false);
        headers = ["#", "رقم الطالب", "اسم الطالب", "المرحلة - الصف", "الحالة", "جوال ولي الأمر", "الرصيد السابق", "إجمالي المستحقات", "الخصومات", "الضريبة", "صافي المستحق", "المسدد", "المتبقي"];
        var index = 1;
        foreach (var student in students)
        {
            var items = accounts.Where(x => x.Contract.StudentId == student.Id).ToList();
            var previous = Net(items.Where(x => x.Categoryid == 10 && x.TransactionTypeId != TransactionTypeIds.FinancialSettlement));
            var dues = Net(items.Where(IsDue));
            var withdrawn = CreditNet(items.Where(x => x.TransactionTypeId == TransactionTypeIds.DuesWithdrawal));
            var discounts = CreditNet(items.Where(x => DiscountTransactionTypes.Contains(x.TransactionTypeId)));
            var tax = Net(items.Where(x => x.Categoryid == 90 && x.TransactionTypeId == TransactionTypeIds.AddDues));
            var paid = CreditNet(items.Where(x => x.TransactionTypeId == TransactionTypeIds.DuesPayment || x.TransactionTypeId == TransactionTypeIds.FeeRefund));
            var net = previous + dues - withdrawn - discounts + tax;
            rows.Add(Row(Num(index++), Text(StudentCode(student)), Text(StudentName(student)), Text($"{LevelName(student)} - {ClassName(student)}"), Text(StatusName(student)), Text(student.Parent?.Mobile), Money(previous), Money(dues), Money(discounts), Money(tax), Money(net), Money(paid), Money(net - paid)));
        }
        AddFinancialSummary(rows, 7, 11, 12);
    }

    private async Task BuildAccountTransactionsAsync(HashSet<Guid> studentIds)
    {
        var accounts = await GetFilteredAccountsAsync(studentIds, applyDateRange: true);
        headers = ["#", "رقم الطالب", "اسم الطالب", "المرحلة", "الصف", "الحالة", "العملية", "الخدمة", "الفصل الدراسي", "تاريخ الحركة", "المرجع", "مدين", "دائن"];
        var index = 1;
        foreach (var item in accounts.OrderBy(x => x.Date).ThenBy(x => x.CreatedOn).ThenBy(x => x.Id))
        {
            var student = item.Contract.Student;
            rows.Add(Row(Num(index++), Text(StudentCode(student)), Text(StudentName(student)), Text(LevelName(student)), Text(ClassName(student)), Text(StatusName(student)), Text(Localized(item.TransactionType.Name, item.TransactionType.NameAr)), Text(Localized(item.Service.Name, item.Service.NameAr)), Text(Localized(item.Semester.Name, item.Semester.NameAr)), Text(ReportFormatter.Date(item.Date)), Text(item.RefNo), Money(item.Debit), Money(item.Credit)));
        }
        summaryCards = [new("إجمالي المدين", Currency(accounts.Sum(x => x.Debit)), "trending_up"), new("إجمالي الدائن", Currency(accounts.Sum(x => x.Credit)), "trending_down")];
    }

    private async Task BuildReceiptsAsync(HashSet<Guid> studentIds)
    {
        headers = ["#", "نوع السند", "رقم السند", "تاريخ السند", "المبلغ", "طريقة الدفع", "رقم الطالب", "اسم الطالب", "اسم الموظف", "تاريخ الإضافة"];
        var paymentMethods = selectedPaymentMethodIds.ToHashSet();
        var receiptDetails = (await _uow.ReceiptDetails.FindAllAsync(x => x.Receipt.Contract.Student.SchoolId == _appStateService.SchoolId && x.Receipt.Contract.YearId == selectedYearId,
            ["Receipt", "Receipt.Contract", "Receipt.Contract.Student", "Receipt.PaymentMethod", "Receipt.User"])).Where(x => selectedVoucherType != "refund" && studentIds.Contains(x.Receipt.Contract.StudentId) && DateMatches(x.Receipt.Date) && Matches(paymentMethods, x.Receipt.PaymentMethodId));
        
        var refundDetails = (await _uow.RefundReceiptDetails.FindAllAsync(x => x.Receipt.Contract.Student.SchoolId == _appStateService.SchoolId && x.Receipt.Contract.YearId == selectedYearId,
            ["Receipt", "Receipt.Contract", "Receipt.Contract.Student", "Receipt.PaymentMethod", "Receipt.User"])).Where(x => selectedVoucherType != "receipt" && studentIds.Contains(x.Receipt.Contract.StudentId) && DateMatches(x.Receipt.Date) && Matches(paymentMethods, x.Receipt.PaymentMethodId));
        var combined = receiptDetails.GroupBy(x => x.ReceiptId).Select(g =>
        {
            var x = g.First(); return new VoucherLine("قبض", x.Receipt.SN, x.Receipt.Date, x.Receipt.Contract.Student, Localized(x.Receipt.PaymentMethod.Name, x.Receipt.PaymentMethod.NameAr), g.Sum(y => y.Amount), x.Receipt.User.Name, x.Receipt.CreatedOn);
        }).Concat(refundDetails.GroupBy(x => x.ReceiptId).Select(g =>
        {
            var x = g.First(); return new VoucherLine("صرف", x.Receipt.SN, x.Receipt.Date, x.Receipt.Contract.Student, Localized(x.Receipt.PaymentMethod.Name, x.Receipt.PaymentMethod.NameAr), g.Sum(y => y.Amount), x.Receipt.User.Name, x.Receipt.CreatedOn);
        })).OrderByDescending(x => x.Date).ThenByDescending(x => x.Number).ToList();
        rows = combined.Select((x, i) => Row(Num(i + 1), Text(x.Type), Num(x.Number), Text(ReportFormatter.Date(x.Date)), Money(x.Amount), Text(x.PaymentMethod), Text(StudentCode(x.Student)), Text(StudentName(x.Student)), Text(x.Employee), Text(ReportFormatter.Date(x.CreatedOn)))).ToList();
        summaryCards = [new("إجمالي القبض", Currency(combined.Where(x => x.Type == "قبض").Sum(x => x.Amount)), "south_west"), new("إجمالي الصرف", Currency(combined.Where(x => x.Type == "صرف").Sum(x => x.Amount)), "north_east"), new("صافي السندات", Currency(combined.Sum(x => x.Type == "قبض" ? x.Amount : -x.Amount)), "payments")];
    }

    private double ReceiptTotalAmount => rows.Where(x => x.Cells.Count > 4).Sum(x => x.Cells[4].Number ?? 0);

    private async Task BuildDiscountsAsync(HashSet<Guid> studentIds)
    {
        var accounts = (await GetFilteredAccountsAsync(studentIds, applyDateRange: true)).Where(x => DiscountTransactionTypes.Contains(x.TransactionTypeId) || x.TransactionTypeId == TransactionTypeIds.FinancialSettlement).ToList();
        headers = ["#", "رقم الطالب", "اسم الطالب", "المرحلة", "الصف", "الحالة", "النسبة", "قيمة الخصم/التسوية", "نوع العملية", "الفصل الدراسي", "الخدمة", "التاريخ", "الملاحظات"];
        rows = accounts.Select((x, i) =>
        {
            var student = x.Contract.Student;
            return Row(Num(i + 1), Text(StudentCode(student)), Text(StudentName(student)), Text(LevelName(student)), Text(ClassName(student)), Text(StatusName(student)), Text($"{x.Percentage * 100m:0.##}%"), Money(x.Credit - x.Debit), Text(Localized(x.TransactionType.Name, x.TransactionType.NameAr)), Text(Localized(x.Semester.Name, x.Semester.NameAr)), Text(Localized(x.Service.Name, x.Service.NameAr)), Text(ReportFormatter.Date(x.Date)), Text(x.Comments));
        }).ToList();
        summaryCards = [new("عدد العمليات", rows.Count.ToString("N0"), "percent"), new("إجمالي الخصومات والتسويات", Currency(accounts.Sum(x => x.Credit - x.Debit)), "sell")];
    }

    private async Task BuildServicePaymentsAsync(HashSet<Guid> studentIds)
    {
        var accounts = await GetFilteredAccountsAsync(studentIds, applyDateRange: true);
        var students = allStudents.Where(x => studentIds.Contains(x.Id)).ToDictionary(x => x.Id);
        headers = ["#", "رقم الطالب", "اسم الطالب", "المرحلة", "الصف", "الحالة", "الجنسية", "جوال ولي الأمر", "الخدمة", "المستحق", "المسدد", "المتبقي"];
        var groups = accounts.GroupBy(x => new { x.Contract.StudentId, x.ServiceId, Service = Localized(x.Service.Name, x.Service.NameAr) })
            .Where(g => g.Any(IsDue) || g.Any(x => x.TransactionTypeId == TransactionTypeIds.DuesPayment || x.TransactionTypeId == TransactionTypeIds.FeeRefund)).ToList();
        var index = 1;
        foreach (var group in groups)
        {
            if (!students.TryGetValue(group.Key.StudentId, out var student)) continue;
            var due = Net(group.Where(IsDue));
            var paid = CreditNet(group.Where(x => x.TransactionTypeId == TransactionTypeIds.DuesPayment || x.TransactionTypeId == TransactionTypeIds.FeeRefund));
            rows.Add(Row(Num(index++), Text(StudentCode(student)), Text(StudentName(student)), Text(LevelName(student)), Text(ClassName(student)), Text(StatusName(student)), Text(Localized(student.Nationality.Name, student.Nationality.NameAr)), Text(student.Parent?.Mobile), Text(group.Key.Service), Money(due), Money(paid), Money(due - paid)));
        }
        AddFinancialSummary(rows, 9, 10, 11);
    }

    private async Task BuildClassroomsAsync(HashSet<Guid> studentIds)
    {
        var selectedRooms = selectedClassRoomIds.ToHashSet();
        var assignments = (await _uow.StudentClassRoomAssignments.FindAllAsync(x => x.Contract.Student.SchoolId == _appStateService.SchoolId && x.Contract.YearId == selectedYearId,
            ["Contract", "Contract.Student", "Contract.Student.Parent", "Contract.Class", "Contract.Class.Level", "ClassRoom", "ClassRoom.Gender"]))
            .Where(x => studentIds.Contains(x.Contract.StudentId) && Matches(selectedRooms, x.ClassRoomId))
            .GroupBy(x => x.Contract.StudentId).ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.UpdatedOn ?? y.CreatedOn).First());
        var receiptDates = (await _uow.Receipts.FindAllAsync(x => x.Contract.Student.SchoolId == _appStateService.SchoolId && x.Contract.YearId == selectedYearId, ["Contract"]))
            .Where(x => studentIds.Contains(x.Contract.StudentId)).GroupBy(x => x.Contract.StudentId).ToDictionary(x => x.Key, x => x.Max(y => y.Date));
        var students = allStudents.Where(x => studentIds.Contains(x.Id)).Where(x => allocationMode switch
        {
            "assigned" => assignments.ContainsKey(x.Id), "unassigned" => !assignments.ContainsKey(x.Id), _ => true
        }).OrderBy(x => x.CurrentContract.Class.LevelId).ThenBy(x => x.CurrentContract.ClassId).ThenBy(x => x.SN).ToList();
        headers = ["#", "رقم الطالب", "اسم الطالب", "المرحلة", "الصف", "الفصل", "حالة الطالب", "تاريخ سداد الرسوم", "تاريخ آخر تحديث"];
        rows = students.Select((student, i) =>
        {
            assignments.TryGetValue(student.Id, out var assignment); receiptDates.TryGetValue(student.Id, out var paidOn);
            return Row(Num(i + 1), Text(StudentCode(student)), Text(StudentName(student)), Text(LevelName(student)), Text(ClassName(student)), Text(assignment is null ? "بدون تسكين" : Localized(assignment.ClassRoom.Name, assignment.ClassRoom.NameAr)), Text(StatusName(student)), Text(paidOn == default ? string.Empty : ReportFormatter.Date(paidOn)), Text(assignment is null ? string.Empty : ReportFormatter.Date(assignment.UpdatedOn ?? assignment.CreatedOn)));
        }).ToList();
        summaryCards = [new("إجمالي الطلاب", rows.Count.ToString("N0"), "groups"), new("المسكنين", students.Count(x => assignments.ContainsKey(x.Id)).ToString("N0"), "meeting_room"), new("بدون تسكين", students.Count(x => !assignments.ContainsKey(x.Id)).ToString("N0"), "person_off")];
    }

    private async Task BuildBusesAsync(HashSet<Guid> studentIds)
    {
        var selectedBuses = selectedBusIds.ToHashSet();
        var assignments = (await _uow.StudentBusAssignments.FindAllAsync(x => x.Subscription.Contract.Student.SchoolId == _appStateService.SchoolId && x.Subscription.Contract.YearId == selectedYearId,
            ["Subscription", "Subscription.Contract", "Subscription.Contract.Student", "Subscription.Contract.Student.Parent", "Subscription.Service", "Bus", "Bus.Type"]))
            .Where(x => studentIds.Contains(x.Subscription.Contract.StudentId) && DateMatches(x.Subscription.SubscriptionDate) && Matches(selectedBuses, x.BusId))
            .GroupBy(x => x.Subscription.Contract.StudentId).ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.UpdatedOn ?? y.CreatedOn).First());
        var busIds = assignments.Values.Select(x => x.BusId).ToHashSet();
        var busStaff = (await _uow.BusAssignments.FindAllAsync(x => x.Bus.SchoolId == _appStateService.SchoolId, ["Bus", "Driver"]))
            .Where(x => busIds.Contains(x.BusId) && (!dateTo.HasValue || x.FromDate.Date <= dateTo.Value.Date) && (!dateFrom.HasValue || x.ToDate.Date >= dateFrom.Value.Date))
            .GroupBy(x => x.BusId).ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.FromDate).First());
        var students = allStudents.Where(x => studentIds.Contains(x.Id)).Where(x => allocationMode switch
        {
            "assigned" => assignments.ContainsKey(x.Id), "unassigned" => !assignments.ContainsKey(x.Id), _ => true
        }).OrderBy(x => x.CurrentContract.Class.LevelId).ThenBy(x => x.CurrentContract.ClassId).ThenBy(x => x.SN).ToList();
        headers = ["#", "رقم الطالب", "اسم الطالب", "المرحلة", "الصف", "حالة الطالب", "الحافلة", "رقم اللوحة", "اسم المشرف", "جوال المشرف", "تاريخ آخر تحديث"];
        rows = students.Select((student, i) =>
        {
            assignments.TryGetValue(student.Id, out var assignment); BusAssignment? staff = null;
            if (assignment is not null) busStaff.TryGetValue(assignment.BusId, out staff);
            return Row(Num(i + 1), Text(StudentCode(student)), Text(StudentName(student)), Text(LevelName(student)), Text(ClassName(student)), Text(StatusName(student)), Text(assignment?.Bus.Name ?? "بدون تسكين"), Text(assignment?.Bus.PlateNo), Text(staff?.SupervisorName), Text(staff?.SupervisorMobile), Text(assignment is null ? string.Empty : ReportFormatter.Date(assignment.UpdatedOn ?? assignment.CreatedOn)));
        }).ToList();
        summaryCards = [new("إجمالي الطلاب", rows.Count.ToString("N0"), "groups"), new("المسكنين", students.Count(x => assignments.ContainsKey(x.Id)).ToString("N0"), "directions_bus"), new("بدون تسكين", students.Count(x => !assignments.ContainsKey(x.Id)).ToString("N0"), "person_off")];
    }

    private async Task BuildServicePricesAsync()
    {
        var levels = selectedLevelIds.ToHashSet();
        var classes = selectedClassIds.ToHashSet();
        var services = selectedServiceIds.ToHashSet();
        var semesters = selectedSemesterIds.ToHashSet();
        var prices = (await _uow.ServicePrices.FindAllAsync(x =>
                x.Class.Level.SchoolId == _appStateService.SchoolId &&
                (x.Service.SchoolId == _appStateService.SchoolId || x.Service.SchoolId == Guid.Empty),
                ["Year", "Service", "Service.Category", "Class", "Class.Level", "Semester"]))
            .Where(x => x.YearId == selectedYearId && Matches(levels, x.Class.LevelId) && Matches(classes, x.ClassId) && Matches(services, x.ServiceId) && Matches(semesters, x.SemesterId))
            .OrderBy(x => x.YearId).ThenBy(x => x.Class.LevelId).ThenBy(x => x.ClassId).ThenBy(x => x.Service.Name).ToList();
        headers = ["#", "الخدمة", "المرحلة", "الصف", "متوفرة خلال", "السعر"];
        rows = prices.Select((x, i) => Row(Num(i + 1), Text(Localized(x.Service.Name, x.Service.NameAr)), Text(Localized(x.Class.Level.Name, x.Class.Level.NameAr)), Text(Localized(x.Class.Name, x.Class.NameAr)), Text(Localized(x.Semester.Name, x.Semester.NameAr)), Money(x.Price))).ToList();
    }

    private async Task<List<StudentAccount>> GetFilteredAccountsAsync(HashSet<Guid> studentIds, bool applyDateRange)
    {
        var services = selectedServiceIds.ToHashSet();
        var semesters = selectedSemesterIds.ToHashSet();
        var transactionTypes = selectedTransactionTypeIds.ToHashSet();
        return (await _uow.StudentAccounts.FindAllAsync(x => x.Contract.Student.SchoolId == _appStateService.SchoolId && x.Contract.YearId == selectedYearId,
            ["Contract", "Contract.Student", "Contract.Student.School", "Contract.Student.CurrentContract", "Contract.Student.CurrentContract.Class", "Contract.Student.CurrentContract.Class.Level", "Contract.Student.CurrentContract.Status", "Service", "Category", "Semester", "TransactionType"]))
            .Where(x => x.ContractId == x.Contract.Student.CurrentContractId
                && studentIds.Contains(x.Contract.StudentId)
                && Matches(services, x.ServiceId)
                && Matches(semesters, x.SemesterId)
                && Matches(transactionTypes, x.TransactionTypeId)
                && (!applyDateRange || DateMatches(x.Date)))
            .ToList();
    }

    private void AddFinancialSummary(List<GenericReportExportRow> reportRows, int duesIndex, int paidIndex, int remainingIndex)
    {
        double SumAt(int index) => reportRows.Sum(x => index >= 0 && index < x.Cells.Count ? x.Cells[index].Number ?? 0 : 0);
        summaryCards = [new("إجمالي المستحقات", Currency(SumAt(duesIndex)), "request_quote"), new("إجمالي المسدد", Currency(SumAt(paidIndex)), "paid"), new("إجمالي المتبقي", Currency(SumAt(remainingIndex)), "account_balance_wallet")];
    }

    private void ConfigureSortOptions()
    {
        sortOptions = activeReportKey switch
        {
            "students" => [new("number", "رقم الطالب", 1), new("name", "اسم الطالب", 2), new("level", "المرحلة", 4), new("status", "الحالة", 6), new("date", "تاريخ الانضمام", 11)],
            "financial-detail" => [new("number", "رقم الطالب", 1), new("name", "اسم الطالب", 2), new("level", "المرحلة", 3), new("remaining", "المتبقي", -1, true)],
            "financial-summary" => [new("number", "رقم الطالب", 1), new("name", "اسم الطالب", 2), new("remaining", "المتبقي", 12, true)],
            "account-transactions" => [new("date", "تاريخ الحركة", 9), new("number", "رقم الطالب", 1), new("name", "اسم الطالب", 2), new("debit", "المدين", 11, true)],
            "receipts" => [new("date", "تاريخ السند", 3), new("receipt", "رقم السند", 2, true), new("name", "اسم الطالب", 7), new("amount", "المبلغ", 4, true)],
            "discounts" => [new("number", "رقم الطالب", 1), new("name", "اسم الطالب", 2), new("amount", "قيمة الخصم", 7, true), new("date", "التاريخ", 11)],
            "service-payments" => [new("number", "رقم الطالب", 1), new("name", "اسم الطالب", 2), new("service", "الخدمة", 8), new("remaining", "المتبقي", 11, true)],
            "classrooms" => [new("number", "رقم الطالب", 1), new("name", "اسم الطالب", 2), new("classroom", "الفصل", 5), new("status", "الحالة", 6)],
            "buses" => [new("number", "رقم الطالب", 1), new("name", "اسم الطالب", 2), new("bus", "الحافلة", 6), new("status", "الحالة", 5)],
            "service-prices" => [new("service", "الخدمة", 1), new("level", "المرحلة", 2), new("class", "الصف", 3), new("price", "السعر", 5, true)],
            _ => [new("number", "الرقم", 1)]
        };
        sortOptions.Add(new("year", "السنة الدراسية", null));
        if (!sortOptions.Any(x => x.Id == selectedSortKey)) selectedSortKey = sortOptions[0].Id;
    }

    private void ApplySort()
    {
        var option = sortOptions.FirstOrDefault(x => x.Id == selectedSortKey) ?? sortOptions.First();
        // Every report is filtered to one required academic year, so all rows
        // have the same year sort key. Preserve their original order in either direction.
        if (option.Id == "year") return;

        var columnIndex = option.ColumnIndex.GetValueOrDefault();
        var index = columnIndex < 0 ? Math.Max(0, headers.Count - 1) : columnIndex;
        IEnumerable<GenericReportExportRow> ordered = option.Numeric
            ? (sortDescending ? rows.OrderByDescending(x => x.Cells.ElementAtOrDefault(index)?.Number ?? 0) : rows.OrderBy(x => x.Cells.ElementAtOrDefault(index)?.Number ?? 0))
            : (sortDescending ? rows.OrderByDescending(x => x.Cells.ElementAtOrDefault(index)?.Text, StringComparer.CurrentCulture) : rows.OrderBy(x => x.Cells.ElementAtOrDefault(index)?.Text, StringComparer.CurrentCulture));
        rows = ordered.Select((row, i) => new GenericReportExportRow(row.Cells.Select((cell, column) => column == 0 ? Num(i + 1) : cell).ToList())).ToList();
    }

    private string BuildFilterSummary()
    {
        var parts = new List<string>();
        if (selectedYearId.HasValue) parts.Add($"السنة الدراسية: {yearOptions.First(x => x.Id == selectedYearId.Value).Name}");
        if (selectedLevelIds.Any()) parts.Add($"{selectedLevelIds.Count()} مرحلة");
        if (selectedClassIds.Any()) parts.Add($"{selectedClassIds.Count()} صف");
        if (selectedStudentIds.Any()) parts.Add($"{selectedStudentIds.Count()} طالب");
        if (selectedServiceIds.Any()) parts.Add($"{selectedServiceIds.Count()} خدمة");
        if (selectedPaymentMethodIds.Any()) parts.Add($"{selectedPaymentMethodIds.Count()} طريقة دفع");
        if (selectedVoucherType != "all") parts.Add(voucherTypeOptions.First(x => x.Id == selectedVoucherType).Name);
        if (selectedTransactionTypeIds.Any()) parts.Add($"{selectedTransactionTypeIds.Count()} نوع حركة");
        if (selectedClassRoomIds.Any()) parts.Add($"{selectedClassRoomIds.Count()} فصل");
        if (selectedBusIds.Any()) parts.Add($"{selectedBusIds.Count()} حافلة");
        if (ShowAllocationFilter && allocationMode != "all") parts.Add(allocationMode == "assigned" ? "المسكنين" : "غير المسكنين");
        if (dateFrom.HasValue || dateTo.HasValue) parts.Add($"الفترة: {(dateFrom.HasValue ? ReportFormatter.Date(dateFrom.Value) : "البداية")} - {(dateTo.HasValue ? ReportFormatter.Date(dateTo.Value) : "اليوم")}");
        return parts.Count == 0 ? "كل البيانات المتاحة" : string.Join(" | ", parts);
    }

    private async Task ExportExcelAsync()
    {
        if (rows.Count == 0 || isExporting) return;
        isExporting = true;
        try
        {
            printedOn = DateTime.UtcNow.GetKsaDateTime();
            var exportRows = IsMonthlyReport ? rows.Append(MonthlyTotalRow()).ToList() : rows;
            if (activeReportKey == "receipts" && rows.Count > 0)
            {
                exportRows = [.. rows, Row(Text(string.Empty), Text(string.Empty), Text(string.Empty), Text("إجمالي المبالغ"), Money(ReceiptTotalAmount), Text(string.Empty), Text(string.Empty), Text(string.Empty), Text(string.Empty), Text(string.Empty))];
            }
            IReadOnlyList<string> exportHeaders = IsMonthlyReport
                ? ["الشهر", "العام الحالي - مستجد", "العام الحالي - منتظم", "العام الحالي - منسحب", "العام الحالي - نشط", "العام القادم - مسجل", "العام القادم - منسحب"]
                : headers;
            var report = new GenericReportExport(ActiveReportTitle, ActiveReportTitle, SchoolName, filterSummary, "تاريخ الطباعة", printedOn, true, exportHeaders, exportRows);
            var bytes = excelExporter.Export(report);
            await _js.InvokeVoidAsync("saveAsFile", $"{activeReportKey}-{DateTime.UtcNow.GetKsaDateTime():yyyyMMdd-HHmm}.xlsx", Convert.ToBase64String(bytes));
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

    private async Task ExportPdfAsync()
    {
        printedOn = DateTime.UtcNow.GetKsaDateTime();
        await InvokeAsync(StateHasChanged);
        await _js.InvokeVoidAsync("APP.printReport", $"{activeReportKey}-{DateTime.UtcNow.GetKsaDateTime():yyyyMMdd-HHmm}", "landscape", "7mm");
    }

    private async Task PrintAsync()
    {
        printedOn = DateTime.UtcNow.GetKsaDateTime();
        await InvokeAsync(StateHasChanged);
        await _js.InvokeVoidAsync("APP.printReport", string.Empty, "landscape", "7mm");
    }

    private bool DateMatches(DateTime value) => (!dateFrom.HasValue || value.Date >= dateFrom.Value.Date) && (!dateTo.HasValue || value.Date <= dateTo.Value.Date);
    private static bool Matches<T>(HashSet<T> values, T value) => values.Count == 0 || values.Contains(value);
    private static bool IsDue(StudentAccount x) =>
        (x.TransactionTypeId == TransactionTypeIds.AddDues || x.TransactionTypeId == TransactionTypeIds.WithdrawalPenalty)
        && x.Categoryid is not (10 or 90);
    private static double Net(IEnumerable<StudentAccount> values) => values.Sum(x => x.Debit - x.Credit);
    private static double CreditNet(IEnumerable<StudentAccount> values) => values.Sum(x => x.Credit - x.Debit);
    private string Localized(string? english, string? arabic) => IsArabic ? (arabic ?? english ?? string.Empty) : (english ?? arabic ?? string.Empty);
    private string StudentCode(Student x) => $"{school?.Code ?? x.School?.Code}-{x.SN:D5}";
    private static string StudentName(Student x) => $"{x.Name} {x.FatherName}".Trim();
    private string LevelName(Student x) => Localized(x.CurrentContract?.Class?.Level?.Name, x.CurrentContract?.Class?.Level?.NameAr);
    private string ClassName(Student x) => Localized(x.CurrentContract?.Class?.Name, x.CurrentContract?.Class?.NameAr);
    private string StatusName(Student x) => Localized(x.CurrentContract?.Status?.Name, x.CurrentContract?.Status?.NameAr);
    private static GenericReportCell Text(string? value) => new(value ?? string.Empty);
    private static GenericReportCell Num(double value) => new(value.ToString("N0"), value);
    private static GenericReportCell Money(double value) => new(ReportFormatter.Amount(value), value);
    private static GenericReportExportRow Row(params GenericReportCell[] cells) => new(cells);
    private static string Currency(double value) => $"{ReportFormatter.Amount(value)} ر.س";

    private sealed record FilterOption<T>(T Id, string Name);
    private sealed record SortOption(string Id, string Name, int? ColumnIndex, bool Numeric = false);
    private sealed record SummaryCard(string Label, string Value, string Icon);
    private sealed record VoucherLine(string Type, int Number, DateTime Date, Student Student, string PaymentMethod, double Amount, string Employee, DateTime CreatedOn);
}
