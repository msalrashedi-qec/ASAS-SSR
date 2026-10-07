using System.Collections.Concurrent;

namespace Web.Services;

/// <summary>
/// Notifies active server-side Blazor circuits that a user's roles or permissions changed.
/// Subscribers are held weakly so completed circuits are not kept alive by this singleton.
/// </summary>
public sealed class AuthorizationUpdateNotifier
{
    private readonly ConcurrentDictionary<Guid, WeakReference<IAuthorizationUpdateSubscriber>> _subscribers = new();
    private readonly ILogger<AuthorizationUpdateNotifier> _logger;

    public AuthorizationUpdateNotifier(ILogger<AuthorizationUpdateNotifier> logger)
    {
        _logger = logger;
    }

    public void Subscribe(IAuthorizationUpdateSubscriber subscriber)
    {
        _subscribers[Guid.NewGuid()] = new WeakReference<IAuthorizationUpdateSubscriber>(subscriber);
    }

    public async Task PublishAsync(IEnumerable<string> userIds)
    {
        var affectedUserIds = userIds.Where(userId => !string.IsNullOrWhiteSpace(userId)).ToHashSet(StringComparer.Ordinal);

        if (affectedUserIds.Count == 0)
            return;

        var notifications = new List<Task>();

        foreach (var subscription in _subscribers)
        {
            if (subscription.Value.TryGetTarget(out var subscriber))
                notifications.Add(NotifySubscriberAsync(subscriber, affectedUserIds));
            else
                _subscribers.TryRemove(subscription.Key, out _);
        }

        await Task.WhenAll(notifications);
    }

    private async Task NotifySubscriberAsync(IAuthorizationUpdateSubscriber subscriber, IReadOnlySet<string> affectedUserIds)
    {
        try
        {
            await subscriber.RefreshAuthorizationAsync(affectedUserIds);
        }
        catch (Exception exception)
        {
            // Authorization was persisted successfully; one disconnected or failing
            // circuit must not make the administrator's save operation fail.
            _logger.LogWarning(exception, "Could not refresh authorization for an active Blazor circuit.");
        }
    }
}

public interface IAuthorizationUpdateSubscriber
{
    Task RefreshAuthorizationAsync(IReadOnlySet<string> affectedUserIds);
}
