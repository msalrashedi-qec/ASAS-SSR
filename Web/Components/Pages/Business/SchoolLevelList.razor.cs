using Microsoft.JSInterop;

namespace Web.Components.Pages.Business
{
    public partial class SchoolLevelList
    {
       
        public SchoolLevelDto _model = new();
        private IEnumerable<SchoolLevelDto> schoolLevels = new List<SchoolLevelDto>();

        private bool isLoading;
        private bool isProcessing;

        protected override async Task OnInitializedAsync()
        {
            await GetListAsync();
        }

        private async Task GetListAsync()
        {
            isLoading = true;
            schoolLevels = _mapper.Map<IEnumerable<SchoolLevelDto>>(await _uow.SchoolLevels.FindAllAsync(x => x.SchoolId == _appStateService.SchoolId));
            schoolLevels = schoolLevels.OrderBy(x => x.SN);
            isLoading = false;
        }

        private void New()
        {
            _model = new SchoolLevelDto();
        }

        private void Edit(SchoolLevelDto item)
        {
            _model = new SchoolLevelDto()
            {
                Id = item.Id,
                Name = item.Name,
                NameAr = item.NameAr,
                SchoolId = item.SchoolId,
                SN = item.SN
            };
        }

        private async Task SaveDataAsync()
        {
            try
            {
                isProcessing = true;

                if (_model.Id == 0)
                {
                    _model.SchoolId = _appStateService.SchoolId;
                    if (await _uow.SchoolLevels.Existing(x => 
                        (x.Name.ToLower() == _model.Name.Trim().ToLower() || 
                         x.NameAr.Trim().ToLower() == _model.NameAr.Trim().ToLower()) &&
                        x.SchoolId == _model.SchoolId))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }

                    var schoolLevel = _mapper.Map<SchoolLevel>(_model);
                    schoolLevel.CreatedBy = await _userService.GetUserIdAsync();
                    schoolLevel.CreatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.SchoolLevels.AddAsync(schoolLevel);
                    if (await _uow.SaveAsync())
                    {
                        _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                        await GetListAsync();
                    }
                    else
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
                }
                else
                {
                    if (await _uow.SchoolLevels.Existing(x => 
                        (x.Name.ToLower() == _model.Name.Trim().ToLower() || 
                         x.NameAr.Trim().ToLower() == _model.NameAr.Trim().ToLower()) &&
                        x.SchoolId == _model.SchoolId &&
                        x.Id != _model.Id))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }

                    var schoolLevel = await _uow.SchoolLevels.FindAsync(x => x.Id == _model.Id);
                    if (schoolLevel is null)
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                        return;
                    }

                    _mapper.Map(_model, schoolLevel);
                    schoolLevel.UpdatedBy = await _userService.GetUserIdAsync();
                    schoolLevel.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.SchoolLevels.Update(schoolLevel);
                    if (await _uow.SaveAsync())
                    {
                        _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                        await GetListAsync();
                    }
                    else
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
                }
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
            }
            finally
            {
                await _js.InvokeVoidAsync("APP.hideModal", "DetailsModal");
                isProcessing = false;
            }
        }

        private async Task DeleteAsync(SchoolLevelDto item)
        {
            try
            {
                isProcessing = true;
                var schoolLevel = await _uow.SchoolLevels.FindAsync(x => x.Id == item.Id);
                if (schoolLevel is null)
                {
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                    return;
                }
                schoolLevel.IsDeleted = true;
                schoolLevel.UpdatedBy = await _userService.GetUserIdAsync();
                schoolLevel.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                await _uow.SchoolLevels.Update(schoolLevel);
                if (await _uow.SaveAsync())
                {
                    _toastService.Notify(NotificationSeverity.Success, _localizer["Delete"], _localizer["DeletedSuccessfully"]);
                    await GetListAsync();
                }
                else
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Delete"], _localizer["Faild_to_delete"]);
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
            }
            finally
            {
                await _js.InvokeVoidAsync("APP.hideModal", "DeleteModal");
                isProcessing = false;
            }
        }
    }
}
