using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Web.Services;

namespace Web.Components.Account
{
    // This is a server-side AuthenticationStateProvider that revalidates the security stamp for the connected user
    // every 30 minutes an interactive circuit is connected.
    internal sealed class IdentityRevalidatingAuthenticationStateProvider
        : RevalidatingServerAuthenticationStateProvider, IAuthorizationUpdateSubscriber
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<IdentityOptions> _options;

        public IdentityRevalidatingAuthenticationStateProvider(
            ILoggerFactory loggerFactory,
            IServiceScopeFactory scopeFactory,
            IOptions<IdentityOptions> options,
            AuthorizationUpdateNotifier authorizationUpdateNotifier)
            : base(loggerFactory)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            authorizationUpdateNotifier.Subscribe(this);
        }

        protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

        protected override async Task<bool> ValidateAuthenticationStateAsync(
            AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            // Get the user manager from a new scope to ensure it fetches fresh data
            await using var scope = _scopeFactory.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            return await ValidateSecurityStampAsync(userManager, authenticationState.User);
        }

        private async Task<bool> ValidateSecurityStampAsync(
            UserManager<ApplicationUser> userManager,
            ClaimsPrincipal principal)
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return false;
            }

            if (!userManager.SupportsUserSecurityStamp)
            {
                return true;
            }

            var principalStamp = principal.FindFirstValue(
                _options.Value.ClaimsIdentity.SecurityStampClaimType);
            var userStamp = await userManager.GetSecurityStampAsync(user);
            return principalStamp == userStamp;
        }

        public async Task RefreshAuthorizationAsync(IReadOnlySet<string> affectedUserIds)
        {
            var authenticationState = await GetAuthenticationStateAsync();
            var userId = authenticationState.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId) || !affectedUserIds.Contains(userId))
            {
                return;
            }

            await using var scope = _scopeFactory.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(userId);

            if (user is null)
            {
                NotifyAuthenticationStateChanged(
                    Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
                return;
            }

            var refreshedPrincipal = await signInManager.CreateUserPrincipalAsync(user);
            NotifyAuthenticationStateChanged(
                Task.FromResult(new AuthenticationState(refreshedPrincipal)));
        }
    }
}
