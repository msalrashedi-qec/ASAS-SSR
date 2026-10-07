namespace Web.Components.Pages.Business
{
    public partial class StudentRejoining
    {
        private readonly StudentService _studentService;

        [Inject]
        private StudentWelcomeMessageService WelcomeMessage { get; set; } = null!;

        public StudentRejoining(StudentService studentService)
        {
           
            _studentService = studentService;
        }

        [Parameter]
        public string? Id { get; set; }



        private bool isLoading;
        private DateTime maxDate = DateTime.UtcNow.GetKsaDateTime();


        private StudentDto _model = new();
        private IEnumerable<YearDto> years = new List<YearDto>();
        private IEnumerable<BasicDto> schoolClasses = new List<BasicDto>();
        private IEnumerable<BasicByteDto> semesters = new List<BasicByteDto>();
        protected override async Task OnInitializedAsync()
        {
            await GetDataAsync();
        }
        
        private async Task GetLookupData()
        {
            try
            {
                isLoading = true;
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
                _model.Semesters.Clear();
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
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task GetDataAsync()
        {
            var student = await _uow.Students.FindAsync(x => x.Id == Guid.Parse(Id), new[] { "School", "CurrentContract", "Nationality" });
            if(student != null && student.CurrentContract.StatusId >= 30)
            {
                _model = _mapper.Map<StudentDto>(student);
                _model.RegisteredForYearId = 0;
                _model.RegistrationDate = null;
                _model.ClassId = 0;
                await GetLookupData();
            }
        }

        private async Task SaveDataAsync()
        {
            try
            {
                isLoading = true;
               
                var student = await _uow.Students.FindAsync(
                    x => x.Id == _model.Id && x.CurrentContract.StatusId >= 30,
                    ["CurrentContract"]);
                if(student == null)
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["NotFound"]);
                    return;
                }
                var registeredForYear = years.First(x => x.Id == _model.RegisteredForYearId);
                var currentYear = years.First(x => x.IsCurrent);

                var discounts = await _uow.DiscountDetails.FindAllAsync(x => x.SchoolId == _model.SchoolId && x.YearId == registeredForYear.Id && x.Discount.ServiceCategoryId == 20 && x.Discount.ApplicationTiming == Shared.Enums.DiscountApplicationTiming.OnSubscription
                         , new[] { "Discount" }
                         , x => x.Discount.SN);

                var userId = await _userService.GetUserIdAsync();
                var contract = await _studentService.CreateContract(_uow, _model, registeredForYear, currentYear, discounts, userId);
                contract.StudentId = student.Id;
                await _studentService.CarryForwardPreviousBalance(_uow, student.CurrentContract, contract, userId);
                var siblingChanges = await _studentService.BuildSiblingDiscountChangesAsync(
                    _uow, student, contract, contract.StartDate, userId, pendingAccounts: contract.StudentAccounts);
                await _studentService.StageSiblingDiscountChangesAsync(_uow, siblingChanges, contract);
                var newContract = await _uow.StudentContracts.AddAsync(contract);

                if (await _uow.SaveAsync())
                {
                    student.CurrentContractId = newContract.Id;
                    await _uow.Students.Update(student);
                    if (!await _uow.SaveAsync())
                    {
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
                        return;
                    }

                    _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                    if (!await WelcomeMessage.SendAsync(_uow, student))
                        _toastService.Notify(NotificationSeverity.Warning, "الرسالة النصية", "تمت إعادة انضمام الطالب، ولكن تعذر إرسال رسالة الترحيب إلى ولي الأمر.");
                    _navigator.NavigateTo("index");
                }
                else
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
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
    }
}
