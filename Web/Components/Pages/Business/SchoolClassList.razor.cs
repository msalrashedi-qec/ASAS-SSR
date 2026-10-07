namespace Web.Components.Pages.Business
{
    public partial class SchoolClassList
    {
        
        public SchoolClassDto _model = new();
        private IEnumerable<SchoolClassDto> schoolClasses = new List<SchoolClassDto>();
        private IEnumerable<SchoolLevelDto> schoolLevels = new List<SchoolLevelDto>();
        private IEnumerable<BasicDto> levelsForSearch = new List<BasicDto>();

        public BasicFilterDto _search = new();

        private bool isLoading;
        private bool isProcessing;

        protected override async Task OnInitializedAsync()
        {
            _search.SchoolId = _appStateService.SchoolId;
            var levels = await _uow.SchoolLevels.FindAllAsync(x => x.SchoolId == _search.SchoolId, x => x.SN);
            schoolLevels = _mapper.Map<IEnumerable<SchoolLevelDto>>(levels);
            levelsForSearch = _mapper.Map<IEnumerable<BasicDto>>(levels);
            await GetListAsync();
        }

        private async Task GetListAsync()
        {
            isLoading = true;
            var list = await _uow.SchoolClasses.FindAllAsync(x =>
            (x.Level.SchoolId == _search.SchoolId)
            && (_search.LevelId == null || x.LevelId == _search.LevelId)
            , new[] { "Level"});
            schoolClasses = _mapper.Map<IEnumerable<SchoolClassDto>>(list).OrderBy(x => x.LevelSN).ThenBy(x => x.SN);
            isLoading = false;
        }

        private void New()
        {
            _model = new SchoolClassDto();
        }

        private void Edit(SchoolClassDto item)
        {
            _model = new SchoolClassDto()
            {
                Id = item.Id,
                Name = item.Name,
                NameAr = item.NameAr,
                LevelId = item.LevelId,
                LevelName = item.LevelName,
                LevelNameAr = item.LevelNameAr,
                SN = item.SN,
                IsGraduationClass = item.IsGraduationClass,
                SchoolId = item.SchoolId
            };
        }

        private async Task SaveDataAsync()
        {
            try
            {
                isProcessing = true;

                if (_model.LevelId == 0)
                {
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["Validation"], _localizer["SelectLevel"]);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_model.Name) || string.IsNullOrWhiteSpace(_model.NameAr))
                {
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["Validation"], _localizer["NameIsRequired"]);
                    return;
                }

                if (_model.Id == 0)
                {
                    if (await _uow.SchoolClasses.Existing(x => 
                        (x.Name.ToLower() == _model.Name.Trim().ToLower() || 
                         x.NameAr.Trim().ToLower() == _model.NameAr.Trim().ToLower()) &&
                        x.LevelId == _model.LevelId))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }

                    var schoolClass = _mapper.Map<SchoolClass>(_model);
                    schoolClass.CreatedBy = await _userService.GetUserIdAsync();
                    schoolClass.CreatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.SchoolClasses.AddAsync(schoolClass);
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
                    if (await _uow.SchoolClasses.Existing(x => 
                        (x.Name.ToLower() == _model.Name.Trim().ToLower() || 
                         x.NameAr.Trim().ToLower() == _model.NameAr.Trim().ToLower()) &&
                        x.LevelId == _model.LevelId &&
                        x.Id != _model.Id))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }

                    var schoolClass = await _uow.SchoolClasses.FindAsync(x => x.Id == _model.Id);
                    if (schoolClass is null)
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                        return;
                    }

                    _mapper.Map(_model, schoolClass);
                    schoolClass.UpdatedBy = await _userService.GetUserIdAsync();
                    schoolClass.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.SchoolClasses.Update(schoolClass);
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

        private async Task DeleteAsync(SchoolClassDto item)
        {
            try
            {
                isProcessing = true;
                var schoolClass = await _uow.SchoolClasses.FindAsync(x => x.Id == item.Id);
                if (schoolClass is null)
                {
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                    return;
                }

                schoolClass.IsDeleted = true;
                schoolClass.UpdatedBy = await _userService.GetUserIdAsync();
                schoolClass.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                await _uow.SchoolClasses.Update(schoolClass);
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
                isProcessing = false;
                await _js.InvokeVoidAsync("APP.hideModal", "DeleteModal");
            }
        }
    }
}
