

using Microsoft.JSInterop;

namespace Web.Components.Partial
{
    public partial class SchoolSelector
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public SchoolSelector(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        protected override async Task OnInitializedAsync()
        {
            await _appStateService.InitializeAsync();
            _appStateService.OnChanged += StateHasChanged;
        }

        public void Dispose()
        {
            _appStateService.OnChanged -= StateHasChanged;
        }

        private Task OpenModalAsync()
        {
            // The SchoolSelectorModal component will load data via OnInitializedAsync
            // Just show the modal - it will handle the data loading
            return _js.InvokeVoidAsync("APP.openSchoolModal").AsTask();
        }
    }
}
