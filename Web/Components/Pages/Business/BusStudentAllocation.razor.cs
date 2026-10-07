namespace Web.Components.Pages.Business;

public partial class BusStudentAllocation
{
    private const int TransportServiceCategoryId = 30;
    private List<Bus> buses = [];
    private List<BusRow> rows = [];
    private Dictionary<int, int> baseOccupancy = [];
    private Dictionary<int, BusAssignment> busAssignments = [];
    private readonly HashSet<Guid> selectedSubscriptionIds = [];
    private int[] transportCategoryIds = [];
    private int selectedBusId;
    private string searchText = "";
    private StudentSortField studentSortField = StudentSortField.LevelGrade;
    private bool sortDescending;
    private bool isLoading;
    private bool isSaving;
    private bool isDraggingSelection;
    private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
    private Bus? SelectedBus => buses.FirstOrDefault(x => x.Id == selectedBusId);
    private BusAssignment? SelectedBusAssignment => busAssignments.GetValueOrDefault(selectedBusId);
    private IEnumerable<BusRow> UnallocatedRows => rows.Where(x => !x.SelectedBusId.HasValue);
    private IEnumerable<BusRow> FilteredUnallocatedRows
    {
        get
        {
            var filtered = string.IsNullOrWhiteSpace(searchText)
                ? UnallocatedRows
                : UnallocatedRows.Where(x => x.StudentName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                    || x.StudentNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase));

            return studentSortField switch
            {
                StudentSortField.Service when sortDescending => filtered.OrderByDescending(x => x.ServiceName, StringComparer.CurrentCulture).ThenBy(x => x.StudentName),
                StudentSortField.Service => filtered.OrderBy(x => x.ServiceName, StringComparer.CurrentCulture).ThenBy(x => x.StudentName),
                _ when sortDescending => filtered.OrderByDescending(x => x.LevelOrder).ThenByDescending(x => x.ClassOrder).ThenBy(x => x.StudentName),
                _ => filtered.OrderBy(x => x.LevelOrder).ThenBy(x => x.ClassOrder).ThenBy(x => x.StudentName)
            };
        }
    }
    private bool AreAllVisibleSelected => FilteredUnallocatedRows.Any() && FilteredUnallocatedRows.All(x => selectedSubscriptionIds.Contains(x.SubscriptionId));
    private bool HasChanges => rows.Any(x => x.SelectedBusId != x.OriginalBusId);
    private int SelectedBusOccupancy => SelectedBus is null ? 0 : BusOccupancy(SelectedBus.Id);
    private int SelectedBusAvailableSeats => SelectedBus is null ? 0 : Math.Max(0, SelectedBus.Capacity - SelectedBusOccupancy);
    private int SelectedBusOccupancyPercent => SelectedBus is null || SelectedBus.Capacity == 0 ? 0 : Math.Min(100, (int)Math.Round(SelectedBusOccupancy * 100d / SelectedBus.Capacity));
    private bool CanAllocateSelection => SelectedBus is not null && selectedSubscriptionIds.Count > 0 && selectedSubscriptionIds.Count <= SelectedBusAvailableSeats;
    private IReadOnlyList<SeatView> SelectedBusSeats => BuildSeatViews();

    protected override async Task OnInitializedAsync()
    {
        await _appStateService.InitializeAsync();
        transportCategoryIds = (await _uow.ServiceCategories.GetAllAsync())
            .Where(x => x.Id == TransportServiceCategoryId
                || x.Name.Contains("transport", StringComparison.OrdinalIgnoreCase)
                || x.NameAr.Contains("نقل", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Id)
            .ToArray();
        buses = (await _uow.Buses.FindAllAsync(x => x.SchoolId == _appStateService.SchoolId, x => x.Name)).ToList();
        var today = DateTime.UtcNow.GetKsaDateTime().Date;
        busAssignments = (await _uow.BusAssignments.FindAllAsync(
            x => x.Bus.SchoolId == _appStateService.SchoolId && x.FromDate <= today && x.ToDate >= today,
            new[] { "Bus", "Driver" }))
            .GroupBy(x => x.BusId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(a => a.FromDate).First());
        selectedBusId = buses.FirstOrDefault()?.Id ?? 0;
        await LoadSubscribersAsync();
    }

    private void BusChanged(ChangeEventArgs args)
    {
        selectedBusId = int.TryParse(args.Value?.ToString(), out var value) ? value : 0;
        isDraggingSelection = false;
        selectedSubscriptionIds.Clear();
    }

    private async Task LoadSubscribersAsync()
    {
        rows = [];
        baseOccupancy = [];
        isDraggingSelection = false;
        selectedSubscriptionIds.Clear();
        isLoading = true;
        try
        {
            var subscriptions = (await _uow.StudentSubscriptions.FindAllAsync(
                x => transportCategoryIds.Contains(x.Service.CategoryId) && x.EndDate == null && x.Contract.Student.SchoolId == _appStateService.SchoolId && x.Contract.Id == x.Contract.Student.CurrentContractId && x.Contract.ActualEndDate == null,
                new[] { "Service", "Semester", "Contract", "Contract.Class", "Contract.Class.Level", "Contract.Student", "Contract.Student.School", "Contract.Student.District" }))
                .GroupBy(x => x.ContractId).Select(x => x.OrderByDescending(s => s.SubscriptionDate).First()).OrderBy(x => x.Contract.Student.Name).ToList();
            var allSchoolAssignments = (await _uow.StudentBusAssignments.FindAllAsync(
                x => x.Subscription.Contract.Student.SchoolId == _appStateService.SchoolId,
                new[]
                {
                    "Subscription.Service", "Subscription.Semester", "Subscription.Contract",
                    "Subscription.Contract.Class", "Subscription.Contract.Class.Level",
                    "Subscription.Contract.Student", "Subscription.Contract.Student.School",
                    "Subscription.Contract.Student.District"
                })).ToList();
            var existing = allSchoolAssignments
                .GroupBy(x => x.Subscription.ContractId)
                .ToDictionary(x => x.Key, x => x.OrderByDescending(a => a.UpdatedOn ?? a.CreatedOn).First());
            baseOccupancy = allSchoolAssignments.GroupBy(x => x.BusId).ToDictionary(x => x.Key, x => x.Select(a => a.Subscription.ContractId).Distinct().Count());

            var subscriptionsByContract = subscriptions.ToDictionary(x => x.ContractId);
            var contractIds = subscriptionsByContract.Keys.Union(existing.Keys);
            rows = contractIds.Select(contractId =>
            {
                var assignment = existing.GetValueOrDefault(contractId);
                var x = assignment?.Subscription ?? subscriptionsByContract[contractId];
                return new BusRow
                {
                    SubscriptionId = x.Id,
                    AssignmentId = assignment?.Id,
                    SelectedBusId = assignment?.BusId,
                    OriginalBusId = assignment?.BusId,
                    StudentNumber = $"{x.Contract.Student.School.Code}-{x.Contract.Student.SN:D5}",
                    StudentName = $"{x.Contract.Student.Name} {x.Contract.Student.FatherName}".Trim(),
                    LevelName = Localized(x.Contract.Class.Level.Name, x.Contract.Class.Level.NameAr),
                    ClassName = Localized(x.Contract.Class.Name, x.Contract.Class.NameAr),
                    LevelOrder = x.Contract.Class.Level.SN,
                    ClassOrder = x.Contract.Class.SN,
                    ServiceName = $"{Localized(x.Service.Name, x.Service.NameAr)} - {Localized(x.Semester.Name, x.Semester.NameAr)}",
                    DistrictName = Localized(x.Contract.Student.District.Name, x.Contract.Student.District.NameAr)
                };
            }).OrderBy(x => x.StudentName).ToList();
        }
        finally { isLoading = false; }
    }

    private int BusOccupancy(int busId) => baseOccupancy.GetValueOrDefault(busId)
        + rows.Count(x => x.SelectedBusId == busId && x.OriginalBusId != busId)
        - rows.Count(x => x.OriginalBusId == busId && x.SelectedBusId != busId);

    private void ToggleStudent(Guid subscriptionId)
    {
        if (!selectedSubscriptionIds.Add(subscriptionId)) selectedSubscriptionIds.Remove(subscriptionId);
    }

    private void ChangeSort(StudentSortField field)
    {
        if (studentSortField == field) sortDescending = !sortDescending;
        else { studentSortField = field; sortDescending = false; }
    }

    private string SortIcon(StudentSortField field) => studentSortField != field
        ? "unfold_more"
        : sortDescending ? "arrow_downward" : "arrow_upward";

    private void ToggleAllVisible(ChangeEventArgs args)
    {
        var shouldSelect = args.Value is bool value && value;
        foreach (var row in FilteredUnallocatedRows.ToList())
        {
            if (shouldSelect) selectedSubscriptionIds.Add(row.SubscriptionId);
            else selectedSubscriptionIds.Remove(row.SubscriptionId);
        }
    }

    private void BeginDraggingSelection(Guid subscriptionId)
    {
        isDraggingSelection = SelectedBus is not null && selectedSubscriptionIds.Contains(subscriptionId);
    }

    private void EndDraggingSelection()
    {
        isDraggingSelection = false;
    }

    private void DropSelected()
    {
        var canAllocate = CanAllocateSelection;
        isDraggingSelection = false;
        if (canAllocate) AllocateSelected();
    }

    private void AllocateSelected()
    {
        var bus = SelectedBus;
        if (bus is null || selectedSubscriptionIds.Count == 0) return;
        if (selectedSubscriptionIds.Count > SelectedBusAvailableSeats)
        {
            _toastService.Notify(NotificationSeverity.Warning, T("Bus capacity", "سعة الحافلة"),
                string.Format(T("Only {0} seats are available on this bus.", "لا يتوفر في هذه الحافلة سوى {0} مقعد."), SelectedBusAvailableSeats));
            return;
        }

        foreach (var row in rows.Where(x => selectedSubscriptionIds.Contains(x.SubscriptionId))) row.SelectedBusId = bus.Id;
        isDraggingSelection = false;
        selectedSubscriptionIds.Clear();
    }

    private void UnallocateStudent(BusRow row)
    {
        row.SelectedBusId = null;
        selectedSubscriptionIds.Remove(row.SubscriptionId);
    }

    private IReadOnlyList<SeatView> BuildSeatViews()
    {
        var bus = SelectedBus;
        if (bus is null || bus.Capacity <= 0) return [];
        var visibleStudents = rows.Where(x => x.SelectedBusId == bus.Id).OrderBy(x => x.StudentName).ToList();
        var hiddenOccupiedCount = Math.Max(0, SelectedBusOccupancy - visibleStudents.Count);
        var seats = new List<SeatView>(bus.Capacity);
        for (var index = 0; index < bus.Capacity; index++)
        {
            var student = index < visibleStudents.Count ? visibleStudents[index] : null;
            var isHiddenOccupied = student is null && index < visibleStudents.Count + hiddenOccupiedCount;
            seats.Add(new SeatView(index + 1, student, isHiddenOccupied, index % 4 == 1));
        }
        return seats;
    }

    private async Task SaveAsync()
    {
        isSaving = true;
        try
        {
            foreach (var bus in buses)
                if (BusOccupancy(bus.Id) > bus.Capacity)
                    throw new InvalidOperationException(string.Format(T("Bus {0} exceeds its capacity of {1}.", "تجاوزت الحافلة {0} سعتها المحددة ({1})."), bus.Name, bus.Capacity));

            var userId = await _userService.GetUserIdAsync();
            var now = DateTime.UtcNow.GetKsaDateTime();
            var changedRows = rows.Where(x => x.SelectedBusId != x.OriginalBusId).ToList();
            if (changedRows.Count == 0)
            {
                _toastService.Notify(NotificationSeverity.Info, T("Allocations", "التسكين"), T("There are no changes to save.", "لا توجد تغييرات للحفظ."));
                return;
            }
            foreach (var row in changedRows)
            {
                if (row.AssignmentId.HasValue)
                {
                    var current = await _uow.StudentBusAssignments.FindAsync(x => x.Id == row.AssignmentId.Value);
                    if (current is null) continue;
                    if (!row.SelectedBusId.HasValue) await _uow.StudentBusAssignments.Delete(current);
                    else { current.BusId = row.SelectedBusId.Value; current.UpdatedBy = userId; current.UpdatedOn = now; await _uow.StudentBusAssignments.Update(current); }
                }
                else if (row.SelectedBusId.HasValue)
                {
                    await _uow.StudentBusAssignments.AddAsync(new StudentBusAssignment { SubscriptionId = row.SubscriptionId, BusId = row.SelectedBusId.Value, CreatedBy = userId, CreatedOn = now });
                }
            }
            if (await _uow.SaveAsync()) _toastService.Notify(NotificationSeverity.Success, T("Save", "حفظ"), T("Allocations saved successfully.", "تم حفظ تسكين الحافلات بنجاح."));
            await LoadSubscribersAsync();
        }
        catch (Exception ex) { _toastService.Notify(NotificationSeverity.Error, T("Error", "خطأ"), ex.Message); }
        finally { isSaving = false; }
    }

    private string T(string en, string ar) => IsArabic ? ar : en;
    private string Localized(string? en, string? ar) => IsArabic ? ar ?? en ?? "" : en ?? ar ?? "";
    
    
    private sealed class BusRow
    {
        public Guid SubscriptionId { get; init; }
        public Guid? AssignmentId { get; init; }
        public int? OriginalBusId { get; init; }
        public int? SelectedBusId { get; set; }
        public string StudentNumber { get; init; } = "";
        public string StudentName { get; init; } = "";
        public string LevelName { get; init; } = "";
        public string ClassName { get; init; } = "";
        public int LevelOrder { get; init; }
        public int ClassOrder { get; init; }
        public string ServiceName { get; init; } = "";
        public string DistrictName { get; init; } = "";
    }
    private enum StudentSortField { LevelGrade, Service }
    private sealed record SeatView(int Number, BusRow? Student, bool IsOccupied, bool IsAisleAfter);
}
