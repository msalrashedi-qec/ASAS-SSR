namespace Web.Components.Pages.Business;

public partial class BusAssignmentList
{
    private BusAssignmentDto model = new();
    private List<BusAssignment> assignments = [];
    private List<Bus> buses = [];
    private List<BusDriver> drivers = [];
    private bool isLoading;
    private bool isProcessing;
    private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

    protected override Task OnInitializedAsync()
    {
        _navigator.NavigateTo("buses", replace: true);
        return Task.CompletedTask;
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        try
        {
            var schoolId = _appStateService.SchoolId;
            buses = (await _uow.Buses.FindAllAsync(x => x.SchoolId == schoolId, x => x.Name)).ToList();
            drivers = (await _uow.BusDrivers.FindAllAsync(x => x.SchoolId == schoolId, x => x.Name)).ToList();
            assignments = (await _uow.BusAssignments.FindAllAsync(x => x.Bus.SchoolId == schoolId && x.Driver.SchoolId == schoolId, new[] { "Bus", "Driver" })).OrderByDescending(x => x.FromDate).ToList();
        }
        finally { isLoading = false; }
    }

    private void New() => model = new BusAssignmentDto();
    private void Edit(BusAssignment assignment) => model = _mapper.Map<BusAssignmentDto>(assignment);

    private async Task SaveAsync()
    {
        isProcessing = true;
        try
        {
            if (model.DriverId == Guid.Empty) throw new InvalidOperationException(T("Select a driver.", "يرجى اختيار السائق."));
            if (model.ToDate.Date < model.FromDate.Date) throw new InvalidOperationException(T("The end date must be on or after the start date.", "يجب أن يكون تاريخ النهاية مساوياً لتاريخ البداية أو بعده."));
            var schoolId = _appStateService.SchoolId;
            if (!await _uow.Buses.Existing(x => x.Id == model.BusId && x.SchoolId == schoolId) || !await _uow.BusDrivers.Existing(x => x.Id == model.DriverId && x.SchoolId == schoolId))
                throw new InvalidOperationException(T("The selected bus or driver does not belong to the current school.", "الحافلة أو السائق المحدد لا يتبع المدرسة الحالية."));
            if (await _uow.BusAssignments.Existing(x => x.Id != model.Id && x.FromDate <= model.ToDate && x.ToDate >= model.FromDate && (x.BusId == model.BusId || x.DriverId == model.DriverId)))
                throw new InvalidOperationException(T("The bus or driver already has an overlapping assignment.", "يوجد إسناد متداخل زمنياً للحافلة أو السائق."));

            var userId = await _userService.GetUserIdAsync();
            var now = DateTime.UtcNow.GetKsaDateTime();
            if (model.Id == Guid.Empty)
            {
                var entity = _mapper.Map<BusAssignment>(model); entity.CreatedBy = userId; entity.CreatedOn = now;
                await _uow.BusAssignments.AddAsync(entity);
            }
            else
            {
                var entity = await _uow.BusAssignments.FindAsync(x => x.Id == model.Id && x.Bus.SchoolId == schoolId) ?? throw new InvalidOperationException(T("Assignment not found.", "الإسناد غير موجود."));
                _mapper.Map(model, entity); entity.UpdatedBy = userId; entity.UpdatedOn = now;
                await _uow.BusAssignments.Update(entity);
            }
            if (await _uow.SaveAsync()) _toastService.Notify(NotificationSeverity.Success, T("Save", "حفظ"), T("Assignment saved successfully.", "تم حفظ الإسناد بنجاح."));
            await LoadAsync();
            await _js.InvokeVoidAsync("APP.hideModal", "AssignmentDetailsModal");
        }
        catch (Exception ex) { _toastService.Notify(NotificationSeverity.Error, T("Error", "خطأ"), ex.Message); }
        finally { isProcessing = false; }
    }

    private string StatusClass(BusAssignment item) => item.ToDate.Date < DateTime.Today ? "bg-secondary" : item.FromDate.Date > DateTime.Today ? "bg-info" : "bg-success";
    private string StatusText(BusAssignment item) => item.ToDate.Date < DateTime.Today ? T("Ended", "منتهي") : item.FromDate.Date > DateTime.Today ? T("Upcoming", "قادم") : T("Active", "نشط");
    private string T(string en, string ar) => IsArabic ? ar : en;
}
