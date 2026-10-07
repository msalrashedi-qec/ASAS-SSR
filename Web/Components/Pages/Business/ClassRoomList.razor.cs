using System.Globalization;
using Core.Entities.Account;
using Core.Entities.Business;
using Core.Entities.General;

namespace Web.Components.Pages.Business;

public partial class ClassRoomList
{
    private List<SchoolLevel> levels = [];
    private List<SchoolClass> classes = [];
    private List<Gender> genders = [];
    private List<ApplicationUser> teachers = [];
    private List<SchoolClassRoom> rooms = [];
    private RoomModel model = new();
    private int filterLevelId;
    private int filterClassId;
    private bool isLoading;
    private bool isProcessing;
    private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
    private IEnumerable<SchoolClass> FilterClasses => classes.Where(x => x.LevelId == filterLevelId);
    private IEnumerable<SchoolClass> ModelClasses => classes.Where(x => x.LevelId == model.LevelId);
    private IEnumerable<SchoolClassRoom> FilteredRooms => rooms.Where(x => filterLevelId == 0 || x.Class.LevelId == filterLevelId).Where(x => filterClassId == 0 || x.ClassId == filterClassId);

    protected override async Task OnInitializedAsync()
    {
        await _appStateService.InitializeAsync();
        levels = (await _uow.SchoolLevels.FindAllAsync(x => x.SchoolId == _appStateService.SchoolId, x => x.SN)).ToList();
        classes = (await _uow.SchoolClasses.FindAllAsync(x => x.Level.SchoolId == _appStateService.SchoolId, new[] { "Level" })).OrderBy(x => x.Level.SN).ThenBy(x => x.SN).ToList();
        genders = (await _uow.Genders.GetAllAsync()).OrderBy(x => x.Id).ToList();
        teachers = (await _uow.UserSchools.FindAllAsync(x => x.SchoolId == _appStateService.SchoolId && x.User.IsActive, new[] { "User" })).Select(x => x.User).DistinctBy(x => x.Id).OrderBy(x => x.Name).ToList();
        await LoadRoomsAsync();
    }

    private async Task LoadRoomsAsync()
    {
        isLoading = true;
        try { rooms = (await _uow.SchoolClassRooms.FindAllAsync(x => x.Class.Level.SchoolId == _appStateService.SchoolId, new[] { "Class", "Class.Level", "Gender", "ResponsibleTeacher" })).OrderBy(x => x.Class.Level.SN).ThenBy(x => x.Class.SN).ThenBy(x => x.Name).ToList(); }
        finally { isLoading = false; }
    }

    private void ListLevelChanged(ChangeEventArgs args) { filterLevelId = int.TryParse(args.Value?.ToString(), out var value) ? value : 0; filterClassId = 0; }
    private void ModelLevelChanged(ChangeEventArgs args) { model.LevelId = int.TryParse(args.Value?.ToString(), out var value) ? value : 0; model.ClassId = 0; }
    private void New() => model = new RoomModel { Capacity = 24 };
    private void Edit(SchoolClassRoom room) => model = new RoomModel { Id = room.Id, LevelId = room.Class.LevelId, ClassId = room.ClassId, Name = room.Name, NameAr = room.NameAr, GenderId = room.GenderId, Capacity = room.Capacity, ResponsibleTeacherId = room.ResponsibleTeacherId };
    private void ConfirmDelete(SchoolClassRoom room) => Edit(room);

    private async Task SaveAsync()
    {
        isProcessing = true;
        try
        {
            model.Name = model.Name.Trim(); model.NameAr = model.NameAr.Trim();
            if (model.LevelId == 0 || model.ClassId == 0 || model.GenderId == 0 || string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.NameAr) || model.Capacity is < 1 or > 100)
                throw new InvalidOperationException(T("Complete all required fields and enter a capacity from 1 to 100.", "أكمل جميع الحقول المطلوبة وأدخل سعة من 1 إلى 100."));
            if (!classes.Any(x => x.Id == model.ClassId && x.LevelId == model.LevelId)) throw new InvalidOperationException(T("The selected grade is invalid.", "الصف المختار غير صالح."));
            if (model.ResponsibleTeacherId is not null && teachers.All(x => x.Id != model.ResponsibleTeacherId)) throw new InvalidOperationException(T("The selected teacher does not belong to this school.", "المعلم/ة المختار لا يتبع هذه المدرسة."));
            if (await _uow.SchoolClassRooms.Existing(x => x.Id != model.Id && x.ClassId == model.ClassId && (x.Name.ToLower() == model.Name.ToLower() || x.NameAr.ToLower() == model.NameAr.ToLower()))) throw new InvalidOperationException(T("A classroom with the same name already exists for this grade.", "توجد فصل بالاسم نفسه في هذا الصف."));
            var userId = await _userService.GetUserIdAsync(); var now = DateTime.UtcNow.GetKsaDateTime();
            if (model.Id == Guid.Empty)
                await _uow.SchoolClassRooms.AddAsync(new SchoolClassRoom { Name = model.Name, NameAr = model.NameAr, ClassId = model.ClassId, GenderId = model.GenderId, Capacity = model.Capacity, ResponsibleTeacherId = model.ResponsibleTeacherId, CreatedBy = userId, CreatedOn = now });
            else
            {
                var room = await _uow.SchoolClassRooms.FindAsync(x => x.Id == model.Id && x.Class.Level.SchoolId == _appStateService.SchoolId) ?? throw new InvalidOperationException(T("Classroom not found.", "الفصل غير موجود."));
                room.Name = model.Name; room.NameAr = model.NameAr; room.ClassId = model.ClassId; room.GenderId = model.GenderId; room.Capacity = model.Capacity; room.ResponsibleTeacherId = model.ResponsibleTeacherId; room.UpdatedBy = userId; room.UpdatedOn = now;
                await _uow.SchoolClassRooms.Update(room);
            }
            if (await _uow.SaveAsync()) _toastService.Notify(NotificationSeverity.Success, T("Save", "حفظ"), T("Classroom saved successfully.", "تم حفظ الفصل بنجاح."));
            await LoadRoomsAsync(); await _js.InvokeVoidAsync("APP.hideModal", "ClassRoomDetailsModal");
        }
        catch (Exception ex) { _toastService.Notify(NotificationSeverity.Error, T("Error", "خطأ"), ex.Message); }
        finally { isProcessing = false; }
    }

    private async Task DeleteAsync()
    {
        isProcessing = true;
        try
        {
            if (await _uow.StudentClassRoomAssignments.Existing(x => x.ClassRoomId == model.Id)) throw new InvalidOperationException(T("Remove the students from this classroom before deleting it.", "ألغِ تسكين الطلاب من هذا الفصل قبل حذفه."));
            var room = await _uow.SchoolClassRooms.FindAsync(x => x.Id == model.Id && x.Class.Level.SchoolId == _appStateService.SchoolId) ?? throw new InvalidOperationException(T("Classroom not found.", "الفصل غير موجودة."));
            room.IsDeleted = true; room.UpdatedBy = await _userService.GetUserIdAsync(); room.UpdatedOn = DateTime.UtcNow.GetKsaDateTime(); await _uow.SchoolClassRooms.Update(room);
            if (await _uow.SaveAsync()) _toastService.Notify(NotificationSeverity.Success, T("Delete", "حذف"), T("Classroom deleted successfully.", "تم حذف الفصل بنجاح."));
            await LoadRoomsAsync(); await _js.InvokeVoidAsync("APP.hideModal", "ClassRoomDeleteModal");
        }
        catch (Exception ex) { _toastService.Notify(NotificationSeverity.Error, T("Error", "خطأ"), ex.Message); }
        finally { isProcessing = false; }
    }

    private string T(string en, string ar) => IsArabic ? ar : en;
    private string Localized(string? en, string? ar) => IsArabic ? ar ?? en ?? "" : en ?? ar ?? "";
    private sealed class RoomModel { public Guid Id { get; set; } public int LevelId { get; set; } public int ClassId { get; set; } public string Name { get; set; } = ""; public string NameAr { get; set; } = ""; public byte GenderId { get; set; } public int Capacity { get; set; } = 24; public string? ResponsibleTeacherId { get; set; } }
}
