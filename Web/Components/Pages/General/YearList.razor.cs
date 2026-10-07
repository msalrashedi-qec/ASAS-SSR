using Core.Entities.General;

namespace Web.Components.Pages.General
{
    public partial class YearList
    {

        public YearDto _model = new();
        public SemesterDto _semester = new();
        private IEnumerable<YearDto> years = new List<YearDto>();
        private IEnumerable<BasicByteDto> semesters = new List<BasicByteDto>();

        private bool isLoading;
        private bool isProcessing;


        protected override async Task OnInitializedAsync()
        {
            await GetListAsync();
            semesters = _mapper.Map<IEnumerable<BasicByteDto>>( await _uow.Semesters.GetAllAsync());
        }

        private async Task GetListAsync()
        {
            isLoading = true;
            var list = (await _uow.Years.FindAllAsync(x => x.Id > 0)).ToList();
            var schoolSemesters = await _uow.SchoolSemesters.FindAllAsync(
                x => x.SchoolId == _appStateService.SchoolId,
                new[] { "Semester" });
            var semestersByYear = schoolSemesters
                .GroupBy(x => x.YearId)
                .ToDictionary(x => x.Key, x => (ICollection<SchoolSemester>)x.ToList());

            foreach (var year in list)
            {
                year.Semesters = semestersByYear.GetValueOrDefault(year.Id)
                    ?? new List<SchoolSemester>();
            }

            years = _mapper.Map<IEnumerable<YearDto>>(list.OrderByDescending(x=> x.Id));
            isLoading = false;
        }
        private void New()
        {
            _model = new YearDto();
        }
        private void Edit(YearDto item)
        {
            _model = new YearDto()
            {
                Id = item.Id,
                Name = item.Name,
                IsCurrent = item.IsCurrent,
                IsNext = item.IsNext,
                Semesters = item.Semesters.OrderBy(x=> x.SemesterId).ToList()
            };
        }

        private void OnCurrentYearChanged(bool isCurrent)
        {
            _model.IsCurrent = isCurrent;
            if (isCurrent)
            {
                _model.IsNext = false;
            }
        }

        private void OnNextYearChanged(bool isNext)
        {
            _model.IsNext = isNext;
            if (isNext)
            {
                _model.IsCurrent = false;
            }
        }

        private void AddToList()
        {
            if(! _model.Semesters.Any(x=> x.SemesterId == _semester.SemesterId))
            {
                _semester.Name = semesters.FirstOrDefault(x => x.Id == _semester.SemesterId)?.Name;
                _semester.NameAr = semesters.FirstOrDefault(x => x.Id == _semester.SemesterId)?.NameAr;
                _model.Semesters.Add(_semester);
                _semester = new();
            }
        }

        private async Task SaveDataAsync()
        {
            try
            {
                isProcessing = true;

                var school = await _uow.Schools.FindAsync(x => x.Id == _appStateService.SchoolId);
                if (school is null)
                {
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], _localizer["SelectSchool"]);
                    return;
                }

                if (_model.Id == 0)
                {
                    if (await _uow.Years.Existing(x => x.Name.ToLower() == _model.Name.Trim().ToLower()))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }
                    


                    if (_model.IsCurrent)
                    {
                        var currentYear = await _uow.Years.FindAsync(x => x.IsCurrent && x.Id != _model.Id);
                        if (currentYear is not null)
                        {
                            currentYear.IsCurrent = false;
                            currentYear.UpdatedBy = await _userService.GetUserIdAsync();
                            currentYear.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();
                            await _uow.Years.Update(currentYear);
                        }
                    }
                    if (_model.IsNext)
                    {
                        var nextYear = await _uow.Years.FindAsync(x => x.IsNext && x.Id != _model.Id);
                        if (nextYear is not null)
                        {
                            nextYear.IsCurrent = false;
                            nextYear.UpdatedBy = await _userService.GetUserIdAsync();
                            nextYear.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();
                            await _uow.Years.Update(nextYear);
                        }
                    }

                    var year = _mapper.Map<Year>(_model);
                    year.CreatedBy = await _userService.GetUserIdAsync();
                    year.CreatedOn = DateTime.UtcNow.GetKsaDateTime();
                    
                    year.Semesters.Clear();
                    foreach(var item in _model.Semesters)
                    {
                        year.Semesters.Add(new SchoolSemester()
                        {
                            SchoolId = school.Id,
                            SemesterId = item.SemesterId,
                            StartDate = item.StartDate,
                            EndDate = item.EndDate,
                            CreatedBy = year.CreatedBy,
                            CreatedOn = year.CreatedOn
                        });
                    };

                    await _uow.Years.AddAsync(year);
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
                    if (await _uow.Years.Existing(x => x.Name.ToLower() == _model.Name.Trim().ToLower() && x.Id != _model.Id))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }
                    var year = await _uow.Years.FindAsync(x => x.Id == _model.Id);
                    if (year is null)
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                        return;
                    }

                    if(_model.IsCurrent)
                    {
                        var currentYear = await _uow.Years.FindAsync(x => x.IsCurrent && x.Id != _model.Id);
                        if (currentYear is not null)
                        {
                            currentYear.IsCurrent = false;
                            currentYear.UpdatedBy = await _userService.GetUserIdAsync();
                            currentYear.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();
                            await _uow.Years.Update(currentYear);
                        }
                    }
                    if(_model.IsNext)
                    {
                        var nextYear = await _uow.Years.FindAsync(x => x.IsNext && x.Id != _model.Id);
                        if (nextYear is not null)
                        {
                            nextYear.IsCurrent = false;
                            nextYear.UpdatedBy = await _userService.GetUserIdAsync();
                            nextYear.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();
                            await _uow.Years.Update(nextYear);
                        }
                    }

                    var existingSemesters = await _uow.SchoolSemesters.FindAllAsync(x =>
                        x.SchoolId == school.Id && x.YearId == year.Id);
                    _uow.SchoolSemesters.DeleteRange(existingSemesters);

                    year.Name = _model.Name;
                    year.IsCurrent = _model.IsCurrent;
                    year.IsNext = _model.IsNext;
                    year.UpdatedBy = await _userService.GetUserIdAsync();
                    year.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                    var replacementSemesters = _model.Semesters
                        .GroupBy(item => item.SemesterId)
                        .Select(group => group.First())
                        .Select(item => new SchoolSemester
                        {
                            SchoolId = school.Id,
                            YearId = year.Id,
                            SemesterId = item.SemesterId,
                            StartDate = item.StartDate,
                            EndDate = item.EndDate,
                            CreatedBy = year.UpdatedBy,
                            CreatedOn = year.UpdatedOn.Value
                        });
                    await _uow.SchoolSemesters.AddRangeAsync(replacementSemesters);

                    await _uow.Years.Update(year);
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
    }
}
