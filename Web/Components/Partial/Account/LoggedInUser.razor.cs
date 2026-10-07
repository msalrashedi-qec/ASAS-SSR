using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Authorization;

namespace Web.Components.Partial.Account
{
    public partial class LoggedInUser
    {
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private IServiceScopeFactory ScopeFactory { get; set; } = default!;
        private string? currentUrl;
        private string? _displayName;

        protected override async Task OnInitializedAsync()
        {
            currentUrl = _navigator.ToBaseRelativePath(_navigator.Uri);
            _navigator.LocationChanged += OnLocationChanged;
            var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();

            // Layout and page components initialize concurrently. Resolve Identity in
            // an independent scope so this lookup doesn't share the page's DbContext.
            await using var scope = ScopeFactory.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.GetUserAsync(authenticationState.User);
            _displayName = user?.Name;
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            currentUrl = _navigator.ToBaseRelativePath(e.Location);
        }

        public void Dispose()
        {
            _navigator.LocationChanged -= OnLocationChanged;
        }

    }
}
