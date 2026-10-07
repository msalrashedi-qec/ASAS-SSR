using System.Globalization;

namespace Web.Services
{
    public class AppStateService
    {
        private readonly UserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _uow;

        private Guid _parentId;
        private Guid _schoolId;
        private string? _schoolLogo;
        private string? _schoolName;
        private bool _isInitialized = false;
        private Task? _initializeTask;

        public event Action? OnChanged;

        public AppStateService(
            UserService userService,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork uow)
        {
            _userService = userService;
            _userManager = userManager;
            _uow = uow;
        }

        public Guid ParentId
        {
            get => _parentId;
            set
            {
                if (_parentId != value)
                {
                    _parentId = value;
                    NotifyStateChanged();
                }
            }
        }

        public Guid SchoolId
        {
            get => _schoolId;
            set
            {
                if (_schoolId != value)
                {
                    _schoolId = value;
                    NotifyStateChanged();
                }
            }
        }

        public string? SchoolLogo
        {
            get => _schoolLogo;
            set
            {
                if (_schoolLogo != value)
                {
                    _schoolLogo = value;
                    NotifyStateChanged();
                }
            }
        }
        public string? SchoolName
        {
            get => _schoolName;
            set
            {
                if (_schoolName != value)
                {
                    _schoolName = value;
                    NotifyStateChanged();
                }
            }
        }


        public Task InitializeAsync()
        {
            if (_isInitialized)
                return Task.CompletedTask;

            return _initializeTask ??= InitializeCoreAsync();
        }

        private async Task InitializeCoreAsync()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                if (user == null)
                    return;

                var permittedSchools = await GetPermittedSchoolsAsync(user);
                var school = permittedSchools.FirstOrDefault(x => x.Id == user.CurrentSchoolId)
                    ?? permittedSchools.FirstOrDefault();

                if (school != null)
                    await ApplySchoolAsync(user, school, user.CurrentSchoolId != school.Id);

                _isInitialized = true;
            }
            finally
            {
                _initializeTask = null;
            }
        }

        public async Task<bool> SetSchoolAsync(Guid schoolId)
        {
            if (schoolId == Guid.Empty)
                return false;

            var user = await GetCurrentUserAsync();
            if (user == null)
                return false;

            var permittedSchools = await GetPermittedSchoolsAsync(user);
            var school = permittedSchools.FirstOrDefault(x => x.Id == schoolId);
            if (school == null)
                return false;

            await ApplySchoolAsync(user, school, true);
            _isInitialized = true;
            return true;
        }

        public bool IsInitialized => _isInitialized;

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var userId = await _userService.GetUserIdAsync();
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            return await _userManager.FindByIdAsync(userId);
        }

        private async Task<IReadOnlyList<School>> GetPermittedSchoolsAsync(ApplicationUser user)
        {
            var permittedSchools = await _uow.UserSchools.FindAllAsync(x => x.UserId == user.Id, new[] { "School" });

            return permittedSchools
                .Where(x => x.School != null)
                .Select(x => x.School)
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .ToList();
        }

        private async Task ApplySchoolAsync(ApplicationUser user, School school, bool persist)
        {
            if (persist && user.CurrentSchoolId != school.Id)
            {
                user.CurrentSchoolId = school.Id;
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                    throw new InvalidOperationException($"Unable to save selected school. {errors}");
                }
            }

            _schoolId = school.Id;
            _schoolLogo = school.Logo;
            _schoolName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar"
                ? school.NameAr
                : school.Name;

            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChanged?.Invoke();

    }
}
