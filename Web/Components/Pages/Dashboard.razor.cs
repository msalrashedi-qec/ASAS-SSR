namespace Web.Components.Pages;

public partial class Dashboard
{
    private bool chartsDirty = true;
    private void InvalidateCharts() => chartsDirty = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (loading || !chartsDirty) return;
        chartsDirty = false;
        var visible = Visible.ToList();
        object Trend(Func<SchoolView, List<Point>> select)
        {
            var months = visible.SelectMany(x => select(x)).Select(x => x.Month).Distinct().Order().ToList();
            return new
            {
                categories = months,
                colors = visible.Select(x => x.Color),
                series = visible.Select(s => new { name = s.Name, data = months.Select(m => select(s).FirstOrDefault(p => p.Month == m)?.Value ?? 0).ToArray() })
            };
        }
        await _js.InvokeVoidAsync("schoolDashboardCharts.render", new
        {
            status = new
            {
                categories = visible.Select(x => x.Name),
                colors = statusOptions.Select((x, i) => string.IsNullOrWhiteSpace(x.ColorCode) ? Colors[i % Colors.Length] : x.ColorCode),
                series = statusOptions.Select(x => new { name = string.IsNullOrWhiteSpace(x.NameAr) ? x.Name : x.NameAr, data = visible.Select(s => s.Statuses.GetValueOrDefault(x.Id)).ToArray() })
            },
            collection = Trend(x => x.Collection),
            activity = Trend(x => x.Activity)
        });
    }

    public async ValueTask DisposeAsync()
    {
        try { await _js.InvokeVoidAsync("schoolDashboardCharts.destroy"); }
        catch (JSDisconnectedException) { }
    }

    private bool loading = true;
    private string? error;
    private string yearName = "—";
    private string selectedSchool = "";
    private DateTime updated;
    private List<SchoolView> schools = [];
    private List<Core.Entities.General.StudentStatus> statusOptions = [];
    private IEnumerable<SchoolView> Visible => schools.Where(x => selectedSchool.Length == 0 || x.Id.ToString() == selectedSchool);
    private static readonly string[] Colors = ["#12a89d", "#6475e8", "#e4a43b", "#e27894", "#3799ce", "#9971cf"];
    private static string Money(decimal value) => value.ToString("N0", CultureInfo.InvariantCulture);
    private decimal Total(Func<SchoolFinanceSummary, decimal> selector) => Visible.Sum(x => selector(x.Finance));
    private decimal Rate => Total(x => x.TotalPaid + x.Remaining) > 0 ? Total(x => x.TotalPaid) / Total(x => x.TotalPaid + x.Remaining) * 100 : 0;
    private sealed record Point(string Month, decimal Value);

    private sealed record SchoolView(Guid Id, string Name, string Code, string Color, SchoolFinanceSummary Finance,
        int Students, int Active, Dictionary<byte, int> Statuses, List<Point> Collection, List<Point> Activity);

    protected override async Task OnInitializedAsync() => await LoadAsync();
    private async Task LoadAsync()
    {
        loading = true;
        error = null;
        schools = [];
        try
        {
            var userId = await _userService.GetUserIdAsync();
            var allowed = (await _uow.UserSchools.FindAllAsync(x => x.UserId == userId && !x.School.IsDeleted, ["School"]))
                .GroupBy(x => x.SchoolId).Select(x => x.First().School).OrderBy(x => x.NameAr).ToList();
            var year = await _uow.Years.FindAsync(x => x.IsCurrent);
            if (year is null) { error = "لم يتم تحديد السنة الدراسية الحالية. يرجى إعدادها أولاً."; return; }
            yearName = year.Name;
            statusOptions = (await _uow.StudentStatuses.FindAllAsync(x=> x.Id > 0)).OrderBy(x => x.Id).ToList();
            var ids = allowed.Select(x => x.Id).ToList();
            if (ids.Count == 0) return;
            var students = (await _uow.Students.FindAllAsync(x => !x.IsDeleted && ids.Contains(x.SchoolId))).ToList();
            var contracts = (await _uow.StudentContracts.FindAllAsync(x => !x.IsDeleted && !x.Student.IsDeleted && x.YearId == year.Id && ids.Contains(x.Student.SchoolId), ["Student", "Status"])).ToList();
            var accounts = (await _uow.StudentAccounts.FindAllAsync(x => !x.IsDeleted && !x.Contract.IsDeleted && !x.Contract.Student.IsDeleted && x.Contract.YearId == year.Id && ids.Contains(x.Contract.Student.SchoolId), ["Contract", "Contract.Student"])).ToList();
            var today = DateTime.UtcNow.GetKsaDateTime().Date;
            var tomorrow = today.AddDays(1);
            var receipts = (await _uow.Receipts.FindAllAsync(x => !x.IsDeleted && !x.Contract.IsDeleted
                && !x.Contract.Student.IsDeleted && x.Contract.YearId == year.Id
                && ids.Contains(x.Contract.Student.SchoolId) && x.Date < tomorrow,
                ["Contract", "Contract.Student", "ReceiptDetails"])).ToList();
            var firstReceipt = receipts.Count == 0 ? (DateTime?)null : receipts.Min(x => x.Date);
            var collectionStart = firstReceipt.HasValue ? new DateTime(firstReceipt.Value.Year, firstReceipt.Value.Month, 1) : (DateTime?)null;
            var collectionByMonth = receipts.GroupBy(x => (x.Contract.Student.SchoolId, x.Date.Year, x.Date.Month))
                .ToDictionary(x => x.Key, x => x.Sum(r => r.ReceiptDetails.Where(d => !d.IsDeleted).Sum(d => (decimal)d.Amount)));
            foreach (var school in allowed)
            {
                var items = contracts.Where(x => x.Student.SchoolId == school.Id).ToList();
                var ledger = accounts.Where(x => x.Contract.Student.SchoolId == school.Id).ToList();
                var currentStudents = students.Where(x => x.SchoolId == school.Id).ToList();
                var statuses = currentStudents.GroupBy(x => x.CurrentStatusId).ToDictionary(x => x.Key, x => x.Count());
                List<Point> collection = [], activity = [];
                if (collectionStart.HasValue)
                {
                    for (var month = collectionStart.Value; month <= today; month = month.AddMonths(1))
                        collection.Add(new(month.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                            collectionByMonth.GetValueOrDefault((school.Id, month.Year, month.Month))));
                }
                activity = MonthlyStudentStatistics.CalculateToCurrentMonth(items, year.Id, today)
                    .Select(x => new Point(x.Month.ToString("yyyy-MM", CultureInfo.InvariantCulture), x.Active)).ToList();
                schools.Add(new(school.Id, string.IsNullOrWhiteSpace(school.NameAr) ? school.Name : school.NameAr, school.Code,
                    Colors[schools.Count % Colors.Length], SchoolDashboardStatistics.Finance(ledger),
                    currentStudents.Count, currentStudents.Count(x => x.CurrentStatusId is 10 or 20), statuses, collection, activity));
            }
            updated = DateTime.UtcNow.GetKsaDateTime();
        }
        catch (Exception)
        {
            error = "تعذر تحميل لوحة البيانات. حاول التحديث مرة أخرى، أو تواصل مع مسؤول النظام.";
        }
        finally { loading = false; InvalidateCharts(); }
    }
}

