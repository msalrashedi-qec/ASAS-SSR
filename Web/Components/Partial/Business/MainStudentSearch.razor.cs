namespace Web.Components.Partial.Business
{
    public partial class MainStudentSearch : IDisposable
    {
        

        [Parameter]
        public string? SchoolId { get; set; }

        [Inject]
        private StudentParentChangeNotificationService StudentParentChanges { get; set; } = null!;


        private ParentDto _parent = new();
        private BasicFilterDto search = new();

        private IEnumerable<StudentListDto> students = new List<StudentListDto>();
        private IEnumerable<ParentListDto> parents = new List<ParentListDto>();
        private bool isLoading = false;

        override protected async Task OnInitializedAsync()
        {
            await _appStateService.InitializeAsync();
            _appStateService.OnChanged += StateHasChanged;
            StudentParentChanges.Changed += OnStudentParentChangedAsync;

        }
        public void Dispose()
        {
            // Unsubscribe when the component is destroyed
            _appStateService.OnChanged -= StateHasChanged;
            StudentParentChanges.Changed -= OnStudentParentChangedAsync;
        }

        override protected async Task OnParametersSetAsync()
        {
            if (string.IsNullOrEmpty(SchoolId) && _appStateService.SchoolId == Guid.Empty)
            {
                _navigator.NavigateTo("select-school");
                return;
            }

            await GetLookupList();
        }

        private async Task GetLookupList()
        {
            try
            {
                isLoading = true;
                if (!await ResolveSelectedSchoolAsync())
                    return;

                if (_appStateService.SchoolId == Guid.Empty) return;
                students = _mapper.Map<IEnumerable<StudentListDto>>(await _uow.Students.FindAllAsync(x => x.SchoolId == _appStateService.SchoolId, new[] { "School" }));
                parents = _mapper.Map<IEnumerable<ParentListDto>>(await _uow.Parents.GetAllAsync());
                search.ParentId = _appStateService.ParentId;
                await SearchParentAsync();
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

        private async Task<bool> ResolveSelectedSchoolAsync()
        {
            if (!string.IsNullOrEmpty(SchoolId))
            {
                if (!Guid.TryParse(SchoolId, out var routeSchoolId))
                {
                    _navigator.NavigateTo("select-school");
                    return false;
                }

                var isSchoolSelected = await _appStateService.SetSchoolAsync(routeSchoolId);
                if (!isSchoolSelected)
                {
                    _navigator.NavigateTo("select-school");
                    return false;
                }
            }
            else
            {
                await _appStateService.InitializeAsync();
            }

            if (_appStateService.SchoolId != Guid.Empty)
                return true;

            _navigator.NavigateTo("select-school");
            return false;
        }

        private async Task SearchStudentAsync()
        {
            var student = students.FirstOrDefault(x => x.Id == search.StudentId);
            if (student == null) return;
            search.ParentId = student.ParentId;
            await SearchParentAsync();
        }

        private async Task SearchParentAsync()
        {
            if (search.ParentId != null)
            {
                try
                {
                    isLoading = true;
                    search.StudentId = null;
                    _parent = _mapper.Map<ParentDto>(await _uow.Parents.FindAsync(x => x.Id == search.ParentId, new[] { "Students", "Students.School", "Students.CurrentContract", "Students.CurrentContract.Class", "Students.CurrentContract.Class.Level", "Students.CurrentStatus" }));
                    if (_parent != null)
                        _parent.Students = _parent.Students.Where(x => x.SchoolId == _appStateService.SchoolId).ToList();

                    _appStateService.ParentId = search.ParentId.Value;
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

        private Task OnStudentParentChangedAsync(StudentParentChangedEvent change)
        {
            if (!IsRelevantChange(change))
                return Task.CompletedTask;

            return InvokeAsync(async () =>
            {
                await GetLookupList();
                StateHasChanged();
            });
        }

        private bool IsRelevantChange(StudentParentChangedEvent change)
        {
            if (change.ChangeType is StudentParentChangeType.ParentCreated or StudentParentChangeType.ParentUpdated)
                return true;

            return change.SchoolId is null
                || change.SchoolId == Guid.Empty
                || change.SchoolId == _appStateService.SchoolId;
        }
    }
}
