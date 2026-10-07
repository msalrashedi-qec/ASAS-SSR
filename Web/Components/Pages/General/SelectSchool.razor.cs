
namespace Web.Components.Pages.General
{
    public partial class SelectSchool
    {
        
        private readonly UserManager<ApplicationUser> _userManager;
        public SelectSchool(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }



        private IEnumerable<SchoolDto> schools = new List<SchoolDto>();
        private Guid? selectedSchoolId;

        private SchoolDto school = new();

        protected override async Task OnInitializedAsync()
        {
            await _appStateService.InitializeAsync();
            await GetListAsync();
        }

        private async Task GetListAsync()
        {
            var user = await _userManager.FindByIdAsync(await _userService.GetUserIdAsync());
            if (user == null)
                return;

            Guid schoolId = Guid.Empty;
            var permittedSchools = await _uow.UserSchools.FindAllAsync(x => x.UserId == user.Id, new[] { "School" });
            schools = _mapper.Map<IEnumerable<SchoolDto>>(permittedSchools.Select(x => x.School).ToList());
            if (schools.Any() && _appStateService.SchoolId != Guid.Empty)
                selectedSchoolId = _appStateService.SchoolId;
            else
                selectedSchoolId = schools.FirstOrDefault()?.Id;
        }
        private async Task SetSelectedSchool()
        {
            if (!selectedSchoolId.HasValue)
                return;

            var isSchoolSelected = await _appStateService.SetSchoolAsync(selectedSchoolId.Value);
            if (isSchoolSelected)
                _navigator.NavigateTo($"index/{_appStateService.SchoolId}");
        }
    }
}
