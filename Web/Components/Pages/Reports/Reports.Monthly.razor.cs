namespace Web.Components.Pages.Reports;

public partial class Reports
{
    private bool IsMonthlyReport => activeReportKey == "monthly-students";
    private List<FilterOption<Guid>> schools = [];
    private Guid selectedSchoolId;
    private async Task LoadMonthlyReferencesAsync(string userId)
    {
        hasRun = false;
        isLoading = true;
        try
        {
            await _appStateService.InitializeAsync();
            schools = (await _uow.UserSchools.FindAllAsync(x => x.UserId == userId, ["School"]))
                .Where(x => x.School != null).GroupBy(x => x.SchoolId)
                .Select(x => new FilterOption<Guid>(x.Key, Localized(x.First().School.Name, x.First().School.NameAr)))
                .OrderBy(x => x.Name).ToList();
            selectedSchoolId = schools.Any(x => x.Id == _appStateService.SchoolId)
                ? _appStateService.SchoolId : schools.FirstOrDefault()?.Id ?? Guid.Empty;
            var years = (await _uow.Years.GetAllAsync()).ToList();
            yearOptions = years.OrderBy(x => x.Name).Select(x => new FilterOption<int>(x.Id, x.Name)).ToList();
            currentYearId = years.FirstOrDefault(x => x.IsCurrent)?.Id;
            selectedYearId = currentYearId;
        }
        catch (Exception ex)
        {
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
        finally { isLoading = false; }
    }

    private GenericReportExportRow MonthlyTotalRow() => Row(Text("الإجمالي"),
        Num(rows.Sum(x => x.Cells.ElementAt(1).Number ?? 0)), Text(string.Empty),
        Num(rows.Sum(x => x.Cells.ElementAt(3).Number ?? 0)), Text(string.Empty),
        Num(rows.Sum(x => x.Cells.ElementAt(5).Number ?? 0)),
        Num(rows.Sum(x => x.Cells.ElementAt(6).Number ?? 0)));

    private async Task RunMonthlyReportAsync()
    {
        hasRun = false;
        rows = [];
        headers = [];
        if (selectedSchoolId == Guid.Empty)
        {
            _toastService.Notify(NotificationSeverity.Warning, "تنبيه", "اختر المدرسة.");
            return;
        }
        isLoading = true;
        try
        {
            var userId = await _userService.GetUserIdAsync();
            if (!await _uow.UserSchools.Existing(x => x.UserId == userId && x.SchoolId == selectedSchoolId))
            {
                _toastService.Notify(NotificationSeverity.Warning, "تنبيه", "لا توجد صلاحية لعرض بيانات المدرسة.");
                return;
            }
            school = await _uow.Schools.FindAsync(x => x.Id == selectedSchoolId);
            if (school is null)
            {
                _toastService.Notify(NotificationSeverity.Warning, "تنبيه", "لم يتم العثور على بيانات المدرسة");
                return;
            }
            var yearId = selectedYearId.Value;
            var contracts = await _uow.StudentContracts.FindAllAsync(x => x.Student.SchoolId == selectedSchoolId && x.YearId == yearId);
            headers = ["الشهر", "مستجد", "منتظم", "منسحب", "نشط", "مسجل للعام القادم", "منسحب"];
            var statistics = MonthlyStudentStatistics.CalculateToCurrentMonth(contracts.ToList(), yearId, DateTime.UtcNow.GetKsaDateTime());
            rows = statistics
                .Select(x => Row(Text(x.Month.ToString("yyyy-MM", CultureInfo.InvariantCulture)), Num(x.New), Num(x.Regular),
                    Num(x.Withdrawn), Num(x.Active), Num(x.NextRegistered), Num(x.NextWithdrawn))).ToList();
            filterSummary = statistics.Count == 0 ? "لا توجد عقود حتى الشهر الحالي في السنة المختارة" : $"الفترة: {statistics[0].Month.ToString("yyyy-MM", CultureInfo.InvariantCulture)} الى {statistics[^1].Month.ToString("yyyy-MM", CultureInfo.InvariantCulture)}";
            printedOn = DateTime.UtcNow.GetKsaDateTime();
            hasRun = true;
        }
        catch (Exception ex) { _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message); }
        finally { isLoading = false; }
    }
}

