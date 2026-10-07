using System.Globalization;

namespace Web.Components.Pages.Business;

public partial class BusList
{
    private BusDto model = new();
    private BusAssignmentDto assignmentModel = new();
    private List<Bus> buses = [];
    private List<BusDriver> drivers = [];
    private List<BusAssignment> busAssignments = [];
    private List<BasicByteDto> types = [];
    private Bus? selectedBus;
    private bool isLoading;
    private bool isProcessing;
    private bool isAssignmentLoading;
    private bool isAssignmentProcessing;
    private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

    protected override async Task OnInitializedAsync()
    {
        await _appStateService.InitializeAsync();
        types = _mapper.Map<List<BasicByteDto>>(await _uow.BusTypes.GetAllAsync());
        drivers = (await _uow.BusDrivers.FindAllAsync(
            x => x.SchoolId == _appStateService.SchoolId,
            x => x.Name)).ToList();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        try { buses = (await _uow.Buses.FindAllAsync(x => x.SchoolId == _appStateService.SchoolId, new[] { "Type" })).OrderBy(x => x.Name).ToList(); }
        finally { isLoading = false; }
    }

    private void New() => model = new BusDto();
    private void Edit(Bus bus) => model = _mapper.Map<BusDto>(bus);

    private async Task OpenAssignmentsAsync(Bus bus)
    {
        selectedBus = bus;
        assignmentModel = new BusAssignmentDto { BusId = bus.Id };
        await LoadAssignmentsAsync();
    }

    private async Task LoadAssignmentsAsync()
    {
        if (selectedBus is null) return;

        isAssignmentLoading = true;
        try
        {
            var schoolId = _appStateService.SchoolId;
            busAssignments = (await _uow.BusAssignments.FindAllAsync(
                x => x.BusId == selectedBus.Id && x.Bus.SchoolId == schoolId,
                new[] { "Driver" }))
                .OrderByDescending(x => x.FromDate)
                .ToList();
        }
        finally { isAssignmentLoading = false; }
    }

    private async Task SaveAssignmentAsync()
    {
        if (selectedBus is null) return;

        isAssignmentProcessing = true;
        try
        {
            assignmentModel.BusId = selectedBus.Id;
            if (assignmentModel.DriverId == Guid.Empty)
                throw new InvalidOperationException(T("Select a driver.", "يرجى اختيار السائق."));
            if (assignmentModel.ToDate.Date < assignmentModel.FromDate.Date)
                throw new InvalidOperationException(T("The end date must be on or after the start date.", "يجب أن يكون تاريخ النهاية مساوياً لتاريخ البداية أو بعده."));

            var schoolId = _appStateService.SchoolId;
            if (!await _uow.Buses.Existing(x => x.Id == selectedBus.Id && x.SchoolId == schoolId) ||
                !await _uow.BusDrivers.Existing(x => x.Id == assignmentModel.DriverId && x.SchoolId == schoolId))
                throw new InvalidOperationException(T("The selected bus or driver does not belong to the current school.", "الحافلة أو السائق المحدد لا يتبع المدرسة الحالية."));

            if (await _uow.BusAssignments.Existing(x =>
                x.DriverId == assignmentModel.DriverId &&
                x.FromDate <= assignmentModel.ToDate &&
                x.ToDate >= assignmentModel.FromDate))
                throw new InvalidOperationException(T("The driver already has an overlapping assignment.", "يوجد إسناد متداخل زمنياً للسائق."));

            if (await _uow.BusAssignments.Existing(x =>
                x.BusId == selectedBus.Id &&
                x.FromDate > assignmentModel.FromDate &&
                x.FromDate <= assignmentModel.ToDate))
                throw new InvalidOperationException(T("The bus has a future assignment that overlaps this period.", "يوجد إسناد مستقبلي للحافلة يتداخل مع هذه الفترة."));

            var userId = await _userService.GetUserIdAsync();
            var now = DateTime.UtcNow.GetKsaDateTime();
            var assignmentStart = assignmentModel.FromDate.Date;
            var assignmentsToClose = (await _uow.BusAssignments.FindAllAsync(x =>
                x.BusId == selectedBus.Id &&
                x.FromDate <= assignmentStart &&
                x.ToDate > assignmentStart)).ToList();

            foreach (var assignment in assignmentsToClose)
            {
                assignment.ToDate = assignmentStart;
                assignment.UpdatedBy = userId;
                assignment.UpdatedOn = now;
            }

            if (assignmentsToClose.Count > 0)
                await _uow.BusAssignments.UpdateRange(assignmentsToClose);

            var entity = _mapper.Map<BusAssignment>(assignmentModel);
            entity.FromDate = assignmentModel.FromDate.Date;
            entity.ToDate = assignmentModel.ToDate.Date;
            entity.CreatedBy = userId;
            entity.CreatedOn = now;
            await _uow.BusAssignments.AddAsync(entity);

            if (await _uow.SaveAsync())
                _toastService.Notify(NotificationSeverity.Success, T("Save", "حفظ"), T("Assignment saved successfully.", "تم حفظ الإسناد بنجاح."));

            assignmentModel = new BusAssignmentDto { BusId = selectedBus.Id };
            await LoadAssignmentsAsync();
        }
        catch (Exception ex) { _toastService.Notify(NotificationSeverity.Error, T("Error", "خطأ"), ex.Message); }
        finally { isAssignmentProcessing = false; }
    }

    private async Task SaveAsync()
    {
        isProcessing = true;
        try
        {
            var plate = model.PlateNo.Trim();
            var vin = model.VIN.Trim();
            if (await _uow.Buses.Existing(x => x.SchoolId == _appStateService.SchoolId && x.Id != model.Id && (x.PlateNo == plate || x.VIN == vin)))
                throw new InvalidOperationException(T("The plate number or VIN is already registered.", "رقم اللوحة أو رقم الهيكل مسجل مسبقاً."));

            var userId = await _userService.GetUserIdAsync();
            var now = DateTime.UtcNow.GetKsaDateTime();
            if (model.Id == 0)
            {
                var entity = _mapper.Map<Bus>(model);
                entity.SchoolId = _appStateService.SchoolId; entity.CreatedBy = userId; entity.CreatedOn = now;
                await _uow.Buses.AddAsync(entity);
            }
            else
            {
                var entity = await _uow.Buses.FindAsync(x => x.Id == model.Id && x.SchoolId == _appStateService.SchoolId) ?? throw new InvalidOperationException(T("Bus not found.", "الحافلة غير موجودة."));
                _mapper.Map(model, entity); entity.UpdatedBy = userId; entity.UpdatedOn = now;
                await _uow.Buses.Update(entity);
            }
            if (await _uow.SaveAsync()) _toastService.Notify(NotificationSeverity.Success, T("Save", "حفظ"), T("Bus saved successfully.", "تم حفظ الحافلة بنجاح."));
            await LoadAsync();
            await _js.InvokeVoidAsync("APP.hideModal", "BusDetailsModal");
        }
        catch (Exception ex) { _toastService.Notify(NotificationSeverity.Error, T("Error", "خطأ"), ex.Message); }
        finally { isProcessing = false; }
    }

    private string T(string en, string ar) => IsArabic ? ar : en;
    private string Localized(string? en, string? ar) => IsArabic ? ar ?? en ?? "" : en ?? ar ?? "";
    private string AssignmentStatusClass(BusAssignment item) => item.ToDate.Date < DateTime.Today ? "bg-secondary" : item.FromDate.Date > DateTime.Today ? "bg-info" : "bg-success";
    private string AssignmentStatusText(BusAssignment item) => item.ToDate.Date < DateTime.Today ? T("Ended", "منتهي") : item.FromDate.Date > DateTime.Today ? T("Upcoming", "قادم") : T("Active", "نشط");
}
