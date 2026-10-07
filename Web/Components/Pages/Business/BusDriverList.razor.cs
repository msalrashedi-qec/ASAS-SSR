namespace Web.Components.Pages.Business;

public partial class BusDriverList
{
    private BusDriverDto model = new();
    private List<BusDriver> drivers = [];
    private List<BasicDto> nationalities = [];
    private bool isLoading;
    private bool isProcessing;
    private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

    protected override async Task OnInitializedAsync()
    {
        await _appStateService.InitializeAsync();
        nationalities = _mapper.Map<List<BasicDto>>(await _uow.Nationalities.GetAllAsync());
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        try { drivers = (await _uow.BusDrivers.FindAllAsync(x => x.SchoolId == _appStateService.SchoolId, new[] { "Nationality" })).OrderBy(x => x.Name).ToList(); }
        finally { isLoading = false; }
    }

    private void New() => model = new BusDriverDto();
    private void Edit(BusDriver driver) => model = _mapper.Map<BusDriverDto>(driver);

    private async Task SaveAsync()
    {
        isProcessing = true;
        try
        {
            var nationalId = model.NationalID.Trim();
            if (await _uow.BusDrivers.Existing(x => x.SchoolId == _appStateService.SchoolId && x.Id != model.Id && x.NationalID == nationalId))
                throw new InvalidOperationException(T("The national ID is already registered.", "رقم الهوية مسجل مسبقاً."));

            var userId = await _userService.GetUserIdAsync();
            var now = DateTime.UtcNow.GetKsaDateTime();
            if (model.Id == Guid.Empty)
            {
                var entity = _mapper.Map<BusDriver>(model);
                entity.SchoolId = _appStateService.SchoolId; entity.CreatedBy = userId; entity.CreatedOn = now;
                await _uow.BusDrivers.AddAsync(entity);
            }
            else
            {
                var entity = await _uow.BusDrivers.FindAsync(x => x.Id == model.Id && x.SchoolId == _appStateService.SchoolId) ?? throw new InvalidOperationException(T("Driver not found.", "السائق غير موجود."));
                _mapper.Map(model, entity); entity.UpdatedBy = userId; entity.UpdatedOn = now;
                await _uow.BusDrivers.Update(entity);
            }
            if (await _uow.SaveAsync()) _toastService.Notify(NotificationSeverity.Success, T("Save", "حفظ"), T("Driver saved successfully.", "تم حفظ بيانات السائق بنجاح."));
            await LoadAsync();
            await _js.InvokeVoidAsync("APP.hideModal", "DriverDetailsModal");
        }
        catch (Exception ex) { _toastService.Notify(NotificationSeverity.Error, T("Error", "خطأ"), ex.Message); }
        finally { isProcessing = false; }
    }

    private string T(string en, string ar) => IsArabic ? ar : en;
    private string Localized(string? en, string? ar) => IsArabic ? ar ?? en ?? "" : en ?? ar ?? "";
}
