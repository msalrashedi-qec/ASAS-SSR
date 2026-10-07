using System.Globalization;
using Core.Entities.Business;

namespace Web.Components.Pages.Business;

public partial class ClassRoomAllocation
{
    private List<SchoolLevel> levels = [];
    private List<SchoolClass> classes = [];
    private List<SchoolClassRoom> rooms = [];
    private List<StudentRow> rows = [];
    private Dictionary<Guid, int> baseOccupancy = [];
    private readonly HashSet<Guid> selectedContractIds = [];
    private int filterLevelId;
    private int filterClassId;
    private Guid selectedRoomId;
    private string studentNumberFilter = "";
    private bool isLoading;
    private bool isSaving;
    private bool isDraggingSelection;
    private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
    private IEnumerable<SchoolClass> FilterClasses => classes.Where(x => x.LevelId == filterLevelId);
    private IEnumerable<SchoolClassRoom> FilteredRooms => rooms.Where(x => filterClassId != 0 && x.ClassId == filterClassId);
    private SchoolClassRoom? SelectedRoom => FilteredRooms.FirstOrDefault(x => x.Id == selectedRoomId);
    private IEnumerable<StudentRow> UnallocatedRows => rows.Where(x => !x.SelectedRoomId.HasValue);
    private IEnumerable<StudentRow> FilteredUnallocatedRows => UnallocatedRows
        .Where(x => filterLevelId == 0 || x.LevelId == filterLevelId)
        .Where(x => filterClassId == 0 || x.ClassId == filterClassId)
        .Where(x => string.IsNullOrWhiteSpace(studentNumberFilter) || x.StudentNumber.Contains(studentNumberFilter.Trim(), StringComparison.OrdinalIgnoreCase))
        .OrderBy(x => x.LevelOrder).ThenBy(x => x.ClassOrder).ThenBy(x => x.StudentName);
    private bool AreAllVisibleSelected => FilteredUnallocatedRows.Any() && FilteredUnallocatedRows.All(x => selectedContractIds.Contains(x.ContractId));
    private bool HasChanges => rows.Any(x => x.SelectedRoomId != x.OriginalRoomId);
    private int SelectedRoomOccupancy => SelectedRoom is null ? 0 : RoomOccupancy(SelectedRoom.Id);
    private int SelectedRoomAvailableSeats => SelectedRoom is null ? 0 : Math.Max(0, SelectedRoom.Capacity - SelectedRoomOccupancy);
    private int SelectedRoomOccupancyPercent => SelectedRoom is null || SelectedRoom.Capacity == 0 ? 0 : Math.Min(100, (int)Math.Round(SelectedRoomOccupancy * 100d / SelectedRoom.Capacity));
    private bool CanAllocateSelection => SelectedRoom is not null && selectedContractIds.Count > 0 && selectedContractIds.Count <= SelectedRoomAvailableSeats;
    private IReadOnlyList<SeatView> SelectedRoomSeats => BuildSeatViews();

    protected override async Task OnInitializedAsync()
    {
        await _appStateService.InitializeAsync();
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        isLoading = true;
        selectedContractIds.Clear();
        try
        {
            levels = (await _uow.SchoolLevels.FindAllAsync(x => x.SchoolId == _appStateService.SchoolId, x => x.SN)).ToList();
            classes = (await _uow.SchoolClasses.FindAllAsync(x => x.Level.SchoolId == _appStateService.SchoolId, new[] { "Level" })).OrderBy(x => x.Level.SN).ThenBy(x => x.SN).ToList();
            rooms = (await _uow.SchoolClassRooms.FindAllAsync(x => x.Class.Level.SchoolId == _appStateService.SchoolId,
                new[] { "Gender", "Class", "Class.Level", "ResponsibleTeacher" })).OrderBy(x => x.Class.Level.SN).ThenBy(x => x.Class.SN).ThenBy(x => x.Name).ToList();
            if (filterClassId == 0) selectedRoomId = Guid.Empty;
            else if (selectedRoomId == Guid.Empty || FilteredRooms.All(x => x.Id != selectedRoomId)) selectedRoomId = FilteredRooms.FirstOrDefault()?.Id ?? Guid.Empty;

            var contracts = (await _uow.StudentContracts.FindAllAsync(
                x => x.Student.SchoolId == _appStateService.SchoolId && x.Id == x.Student.CurrentContractId && x.ActualEndDate == null,
                new[] { "Student", "Student.School", "Student.Gender", "Class", "Class.Level" })).OrderBy(x => x.Student.Name).ToList();
            var contractIds = contracts.Select(x => x.Id).ToHashSet();
            var assignments = (await _uow.StudentClassRoomAssignments.FindAllAsync(x => contractIds.Contains(x.ContractId))).GroupBy(x => x.ContractId).ToDictionary(x => x.Key, x => x.First());
            var allAssignments = await _uow.StudentClassRoomAssignments.FindAllAsync(x => x.Contract.Student.SchoolId == _appStateService.SchoolId, new[] { "Contract", "Contract.Student" });
            baseOccupancy = allAssignments.GroupBy(x => x.ClassRoomId).ToDictionary(x => x.Key, x => x.Select(a => a.ContractId).Distinct().Count());

            rows = contracts.Select(x => new StudentRow
            {
                ContractId = x.Id,
                AssignmentId = assignments.GetValueOrDefault(x.Id)?.Id,
                SelectedRoomId = assignments.GetValueOrDefault(x.Id)?.ClassRoomId,
                OriginalRoomId = assignments.GetValueOrDefault(x.Id)?.ClassRoomId,
                StudentNumber = $"{x.Student.School.Code}-{x.Student.SN:D5}",
                StudentName = $"{x.Student.Name} {x.Student.FatherName}".Trim(),
                GenderId = x.Student.GenderId,
                GenderName = Localized(x.Student.Gender.Name, x.Student.Gender.NameAr),
                LevelId = x.Class.LevelId,
                ClassId = x.ClassId,
                LevelName = Localized(x.Class.Level.Name, x.Class.Level.NameAr),
                ClassName = Localized(x.Class.Name, x.Class.NameAr),
                LevelOrder = x.Class.Level.SN,
                ClassOrder = x.Class.SN
            }).ToList();
        }
        finally { isLoading = false; }
    }

    private void FilterLevelChanged(ChangeEventArgs args)
    {
        filterLevelId = int.TryParse(args.Value?.ToString(), out var value) ? value : 0;
        filterClassId = 0;
        selectedRoomId = Guid.Empty;
        isDraggingSelection = false;
        selectedContractIds.Clear();
    }

    private void FilterClassChanged(ChangeEventArgs args)
    {
        filterClassId = int.TryParse(args.Value?.ToString(), out var value) ? value : 0;
        selectedRoomId = FilteredRooms.FirstOrDefault()?.Id ?? Guid.Empty;
        isDraggingSelection = false;
        selectedContractIds.Clear();
    }

    private void RoomChanged(ChangeEventArgs args)
    {
        var roomId = Guid.TryParse(args.Value?.ToString(), out var value) ? value : Guid.Empty;
        selectedRoomId = FilteredRooms.Any(x => x.Id == roomId) ? roomId : Guid.Empty;
        isDraggingSelection = false;
        selectedContractIds.Clear();
    }

    private void ToggleStudent(Guid contractId)
    {
        if (!selectedContractIds.Add(contractId)) selectedContractIds.Remove(contractId);
    }

    private void ToggleAllVisible(ChangeEventArgs args)
    {
        var shouldSelect = args.Value is bool value && value;
        foreach (var row in FilteredUnallocatedRows.ToList())
            if (shouldSelect) selectedContractIds.Add(row.ContractId); else selectedContractIds.Remove(row.ContractId);
    }

    private void BeginDraggingSelection(Guid contractId)
    {
        isDraggingSelection = SelectedRoom is not null && selectedContractIds.Contains(contractId);
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
        var room = SelectedRoom;
        if (room is null || selectedContractIds.Count == 0) return;
        var selectedRows = rows.Where(x => selectedContractIds.Contains(x.ContractId)).ToList();
        if (selectedRows.Any(x => x.ClassId != room.ClassId))
        {
            _toastService.Notify(NotificationSeverity.Warning, T("Grade mismatch", "اختلاف الصف"), T("All selected students must belong to the classroom grade.", "يجب أن ينتمي جميع الطلاب المحددين إلى صف الفصل المختار."));
            return;
        }
        if (selectedRows.Any(x => x.GenderId != room.GenderId && room.GenderId != 10))
        {
            _toastService.Notify(NotificationSeverity.Warning, T("Gender mismatch", "اختلاف الجنس"), T("All selected students must match the classroom gender.", "يجب أن يتوافق جنس جميع الطلاب المحددين مع الفصل المختار."));
            return;
        }
        if (selectedRows.Count > SelectedRoomAvailableSeats)
        {
            _toastService.Notify(NotificationSeverity.Warning, T("Classroom capacity", "سعة الفصل"), string.Format(T("Only {0} seats are available.", "لا يتوفر سوى {0} مقعد."), SelectedRoomAvailableSeats));
            return;
        }
        foreach (var row in selectedRows) row.SelectedRoomId = room.Id;
        isDraggingSelection = false;
        selectedContractIds.Clear();
    }

    private void UnallocateStudent(StudentRow row)
    {
        row.SelectedRoomId = null;
        selectedContractIds.Remove(row.ContractId);
    }

    private int RoomOccupancy(Guid roomId) => baseOccupancy.GetValueOrDefault(roomId)
        + rows.Count(x => x.SelectedRoomId == roomId && x.OriginalRoomId != roomId)
        - rows.Count(x => x.OriginalRoomId == roomId && x.SelectedRoomId != roomId);

    private IReadOnlyList<SeatView> BuildSeatViews()
    {
        var room = SelectedRoom;
        if (room is null || room.Capacity <= 0) return [];
        var visibleStudents = rows.Where(x => x.SelectedRoomId == room.Id).OrderBy(x => x.StudentName).ToList();
        var hiddenOccupiedCount = Math.Max(0, SelectedRoomOccupancy - visibleStudents.Count);
        var seats = new List<SeatView>(room.Capacity);
        for (var index = 0; index < room.Capacity; index++)
        {
            var student = index < visibleStudents.Count ? visibleStudents[index] : null;
            seats.Add(new SeatView(index + 1, student, student is null && index < visibleStudents.Count + hiddenOccupiedCount));
        }
        return seats;
    }

    private async Task SaveAsync()
    {
        isSaving = true;
        try
        {
            foreach (var room in rooms)
                if (RoomOccupancy(room.Id) > room.Capacity)
                    throw new InvalidOperationException(string.Format(T("Classroom {0} exceeds its capacity of {1}.", "تجاوز الفصل {0} سعته المحددة ({1})."), RoomDisplayName(room), room.Capacity));
            var invalid = rows.FirstOrDefault(row => row.SelectedRoomId.HasValue && rooms.FirstOrDefault(room => room.Id == row.SelectedRoomId.Value) is { } room && (room.GenderId != row.GenderId || room.ClassId != row.ClassId));
            if (invalid is not null) throw new InvalidOperationException(T("A selected classroom does not match the student's grade or gender.", "إحد الفصول المحددة لا يتوافق مع صف الطالب أو جنسه."));

            var changedRows = rows.Where(x => x.SelectedRoomId != x.OriginalRoomId).ToList();
            if (changedRows.Count == 0) { _toastService.Notify(NotificationSeverity.Info, T("Allocations", "التسكين"), T("There are no changes to save.", "لا توجد تغييرات للحفظ.")); return; }
            var userId = await _userService.GetUserIdAsync();
            var now = DateTime.UtcNow.GetKsaDateTime();
            foreach (var row in changedRows)
            {
                if (row.AssignmentId.HasValue)
                {
                    var current = await _uow.StudentClassRoomAssignments.FindAsync(x => x.Id == row.AssignmentId.Value);
                    if (current is null) continue;
                    if (!row.SelectedRoomId.HasValue) await _uow.StudentClassRoomAssignments.Delete(current);
                    else { current.ClassRoomId = row.SelectedRoomId.Value; current.UpdatedBy = userId; current.UpdatedOn = now; await _uow.StudentClassRoomAssignments.Update(current); }
                }
                else if (row.SelectedRoomId.HasValue)
                    await _uow.StudentClassRoomAssignments.AddAsync(new StudentClassRoomAssignment { ContractId = row.ContractId, ClassRoomId = row.SelectedRoomId.Value, CreatedBy = userId, CreatedOn = now });
            }
            if (await _uow.SaveAsync()) _toastService.Notify(NotificationSeverity.Success, T("Save", "حفظ"), T("Classroom allocations saved successfully.", "تم حفظ تسكين الفصول بنجاح."));
            await LoadDataAsync();
        }
        catch (Exception ex) { _toastService.Notify(NotificationSeverity.Error, T("Error", "خطأ"), ex.Message); }
        finally { isSaving = false; }
    }

    private string RoomDisplayName(SchoolClassRoom room) => $"{Localized(room.Name, room.NameAr)} · {Localized(room.Class.Name, room.Class.NameAr)}";
    private string T(string en, string ar) => IsArabic ? ar : en;
    private string Localized(string? en, string? ar) => IsArabic ? ar ?? en ?? "" : en ?? ar ?? "";
    private sealed class StudentRow
    {
        public Guid ContractId { get; init; }
        public Guid? AssignmentId { get; init; }
        public Guid? OriginalRoomId { get; init; }
        public Guid? SelectedRoomId { get; set; }
        public string StudentNumber { get; init; } = "";
        public string StudentName { get; init; } = "";
        public byte GenderId { get; init; }
        public string GenderName { get; init; } = "";
        public int LevelId { get; init; }
        public int ClassId { get; init; }
        public string LevelName { get; init; } = "";
        public string ClassName { get; init; } = "";
        public int LevelOrder { get; init; }
        public int ClassOrder { get; init; }
    }
    private sealed record SeatView(int Number, StudentRow? Student, bool IsOccupied);
}
