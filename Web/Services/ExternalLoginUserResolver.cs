
namespace Web.Services;

public sealed class ExternalLoginUserResolver(
    IServiceScopeFactory serviceScopeFactory)
{
    private static readonly TimeSpan LookupTimeout = TimeSpan.FromSeconds(15);

    public async Task<ApplicationUser?> FindAsync( string loginProvider, string providerKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(loginProvider) || string.IsNullOrWhiteSpace(providerKey))
        {
            return null;
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(LookupTimeout);

        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var userStore = scope.ServiceProvider.GetRequiredService<IUserStore<ApplicationUser>>();

        if (userStore is not IUserLoginStore<ApplicationUser> loginStore)
        {
            throw new NotSupportedException(
                "The configured Identity user store does not support external logins.");
        }

        return await loginStore.FindByLoginAsync( loginProvider, providerKey, timeoutSource.Token);
    }
}
