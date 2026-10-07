namespace Web.Components.Pages.Business
{
    public partial class StudentDetails
    {
        
        private readonly StudentService _studentService;
        public StudentDetails(StudentService studentService)
        {
            _studentService = studentService;
        }

        [Parameter]
        public string? Id { get; set; }

        [Parameter]
        public string? ParentId { get; set; }

        [Inject]
        private StudentParentChangeNotificationService StudentParentChanges { get; set; } = null!;

        [Inject]
        private StudentWelcomeMessageService WelcomeMessage { get; set; } = null!;


        private bool isLoading;
        private DateTime maxDate = DateTime.UtcNow.GetKsaDateTime();
        private string studentAge = string.Empty;

        private ParentDto _parent = new();
        private StudentDto _model = new();
        private IEnumerable<BasicByteDto> genders = new List<BasicByteDto>();
        private IEnumerable<BasicDto> nationalities = new List<BasicDto>();
        private IEnumerable<BasicDto> districts = new List<BasicDto>();
        private IEnumerable<YearDto> years = new List<YearDto>();
        private IEnumerable<BasicDto> schoolClasses = new List<BasicDto>();
        private IEnumerable<BasicByteDto> semesters = new List<BasicByteDto>();



        protected override async Task OnInitializedAsync()
        {
            _model.SchoolId = _appStateService.SchoolId;
            await GetLookupData();
            await GetDataAsync();
        }

        private async Task GetParentAsync()
        {
            try
            {
                if (ParentId != null)
                {
                    var parent = await _uow.Parents.FindAsync(x => x.Id == Guid.Parse(ParentId));
                    if (parent != null)
                    {
                        _parent.Name = parent.Name;
                        _parent.NationalID = parent.NationalID;
                        _parent.Mobile = parent.Mobile;
                        _parent.HomeAddress = parent.HomeAddress;

                        _model.ParentId = parent.Id;
                        _model.FatherName = parent.Name;
                    }
                }
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
            }

        }

        private async Task GetLookupData()
        {
            try
            {
                isLoading = true;
                genders = _mapper.Map<IEnumerable<BasicByteDto>>(await _uow.Genders.FindAllAsync(x => x.Id < 3));
                nationalities = _mapper.Map<IEnumerable<BasicDto>>(await _uow.Nationalities.GetAllAsync());
                districts = _mapper.Map<IEnumerable<BasicDto>>(await _uow.Districts.GetAllAsync());
                years = _mapper.Map<IEnumerable<YearDto>>(await _uow.Years.FindAllAsync(x => true, new[] { "Semesters" }));
                schoolClasses = _mapper.Map<IEnumerable<BasicDto>>(await _uow.SchoolClasses.FindAllAsync(x => x.Level.SchoolId == _model.SchoolId, new[] { "Level" }));
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task OnYearChanged()
        {
            try
            {
                isLoading = true;
                if (_model.RegisteredForYearId > 0 && _model.SchoolId != Guid.Empty)
                {
                    semesters = (_mapper.Map<IEnumerable<BasicByteDto>>(await _uow.SchoolSemesters.FindAllAsync(x => x.SchoolId == _model.SchoolId && x.YearId == _model.RegisteredForYearId && x.SemesterId > 0, new[] { "Semester" }))).OrderBy(x => x.Id);
                    _model.StatusId = years.First(x => x.Id == _model.RegisteredForYearId).IsCurrent ? (byte)10 : (byte)5;
                }
                else
                    semesters = new List<BasicByteDto>();
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.ToString());
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task GetDataAsync()
        {
            try
            {
                if (Id != null)
                {
                    _model = _mapper.Map<StudentDto>(await _uow.Students.FindAsync(x => x.Id == Guid.Parse(Id), new[] { "School", "Parent", "CurrentContract", "CurrentContract.Details", "CurrentContract.Status" }));
                    if (_model != null)
                    {
                        _parent.Name = _model.ParentName;
                        _parent.NationalID = _model.ParentNationalID;
                        _parent.Mobile = _model.ParentMobile;
                        _parent.HomeAddress = _model.ParentAddress;
                        await OnYearChanged();
                        CalculateAge();
                    }
                }
                else
                    await GetParentAsync();
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
            }

        }

        private async Task SaveDataAsync()
        {
            try
            {
                isLoading = true;

                if (_model.Id == Guid.Empty)
                {
                    var changeType = StudentParentChangeType.StudentCreated;

                    if (await _uow.Students.Existing(x => x.NationalID == _model.NationalID.Trim()))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }
                    if(_model.SchoolId == Guid.Empty && _appStateService.SchoolId != Guid.Empty)
                        _model.SchoolId = _appStateService.SchoolId;
                    if (_model.SchoolId == Guid.Empty)
                        _navigator.NavigateTo("select-school");
                    var student = _mapper.Map<Student>(_model);
                    student.SN = await _studentService.GenerateStudentNumber(_uow, _model.SchoolId);
                    student.CreatedBy = await _userService.GetUserIdAsync();
                    student.CreatedOn = DateTime.UtcNow.GetKsaDateTime();

                    var registeredForYear = years.First(x => x.Id == _model.RegisteredForYearId);
                    var currentYear = years.First(x => x.IsCurrent);

                    // Get discounts for the student based on the registered year and school
                    var discounts = await _uow.DiscountDetails.FindAllAsync( x => x.SchoolId == _model.SchoolId && x.YearId == registeredForYear.Id && x.Discount.TypeId == 50 && x.Discount.ServiceCategoryId == 20 && x.Discount.ApplicationTiming == Shared.Enums.DiscountApplicationTiming.OnSubscription,
                        new[] { "Discount" },
                        x => x.Discount.SN);

                    var contract = await _studentService.CreateContract(_uow, _model, registeredForYear, currentYear, discounts, student.CreatedBy);
                    var siblingChanges = await _studentService.BuildSiblingDiscountChangesAsync(_uow, student, contract, contract.StartDate, student.CreatedBy, pendingAccounts: contract.StudentAccounts);
                    await _studentService.StageSiblingDiscountChangesAsync(_uow, siblingChanges, contract);
                    student.Contracts.Add(contract);

                    await _uow.Students.AddAsync(student);
                    if (await _uow.SaveAsync())
                    {
                        student.CurrentContractId = contract.Id;
                        student.CurrentStatusId = contract.StatusId;
                        await _uow.Students.Update(student);
                        if (await _uow.SaveAsync())
                        {
                            _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                            await StudentParentChanges.NotifyAsync(new StudentParentChangedEvent(changeType, student.SchoolId, student.ParentId, student.Id));
                            if (!await WelcomeMessage.SendAsync(_uow, student))
                                _toastService.Notify(NotificationSeverity.Warning, "الرسالة النصية", "تعذر إرسال رسالة الترحيب إلى ولي الأمر.");
                            _navigator.NavigateTo($"index/{_model.SchoolId}");
                        }
                        else
                            _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
                    }
                    else
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
                }
                else
                {
                    var changeType = StudentParentChangeType.StudentUpdated;

                    if (await _uow.Students.Existing(x => x.NationalID == _model.NationalID.Trim() && x.Id != _model.Id))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }

                    var student = await _uow.Students.FindAsync(x => x.Id == _model.Id);
                    if (student is null)
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                        return;
                    }

                    var currentContract = await _uow.StudentContracts.FindAsync(
                        x => x.Id == student.CurrentContractId);
                    if (currentContract is null)
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                        return;
                    }

                    var currentDetails = await _uow.StudentContractDetails.FindAllAsync(x => x.ContractId == currentContract.Id && x.ServiceId == StudentService.StudyServiceId);
                    var currentSemesters = currentDetails.Select(x => x.SemesterId).Distinct().OrderBy(x => x);
                    var requestedSemesters = _model.Semesters.Distinct().OrderBy(x => x);

                    var recalculateFinancials = currentContract.StartDate.Date != _model.RegistrationDate?.Date
                        || currentContract.RegisteredForYearId != _model.RegisteredForYearId
                        || currentContract.ClassId != _model.ClassId
                        || !currentSemesters.SequenceEqual(requestedSemesters);

                    _mapper.Map(_model, student);
                    var userId = await _userService.GetUserIdAsync();
                    student.UpdatedBy = userId;
                    student.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                    if (recalculateFinancials)
                    {
                        var registeredForYear = years.First(x => x.Id == _model.RegisteredForYearId);
                        var currentYear = years.First(x => x.IsCurrent);
                        var discounts = await _uow.DiscountDetails.FindAllAsync(x => x.SchoolId == _model.SchoolId && x.YearId == registeredForYear.Id && x.Discount.ServiceCategoryId == 20 && x.Discount.ApplicationTiming == Shared.Enums.DiscountApplicationTiming.OnSubscription
                        , new[] { "Discount" }
                        , x => x.Discount.SN);

                        await _studentService.RecalculateContractFinancials(_uow, currentContract.Id, _model, registeredForYear, currentYear, discounts, userId);
                    }

                    await _uow.Students.Update(student);
                    if (await _uow.SaveAsync())
                    {
                        _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                        await StudentParentChanges.NotifyAsync(new StudentParentChangedEvent(changeType, student.SchoolId, student.ParentId, student.Id));
                    }
                    else
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
                }
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.ToString());
            }
            finally
            {
                isLoading = false;
            }
        }

        private void CalculateAge()
        {
            if (_model.BirthDate != DateTime.MinValue)
                studentAge = AgeCalculator.Calculate(_model.BirthDate).ToString();
            else
                studentAge = string.Empty;
        }
    }
}
