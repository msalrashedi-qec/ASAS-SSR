namespace Core.Services;

public enum WorkflowUpdateKind
{
    Changed,
    ProcessCreated
}

public sealed record WorkflowUpdate(WorkflowUpdateKind Kind, DateTime OccurredOn);

/// <summary>
/// Broadcasts workflow changes to the connected Blazor Server circuits.
/// Database persistence is never made dependent on a UI subscriber.
/// </summary>
public sealed class WorkflowUpdateNotifier
{
    public event Func<WorkflowUpdate, Task>? Updated;

    public void Publish(WorkflowUpdateKind kind)
    {
        var update = new WorkflowUpdate(kind, DateTime.UtcNow);
        var subscribers = Updated?.GetInvocationList()
            .Cast<Func<WorkflowUpdate, Task>>()
            .ToArray() ?? [];

        foreach (var subscriber in subscribers)
            _ = NotifySafelyAsync(subscriber, update);
    }

    private static async Task NotifySafelyAsync(
        Func<WorkflowUpdate, Task> subscriber,
        WorkflowUpdate update)
    {
        try
        {
            await subscriber(update);
        }
        catch
        {
            // A disconnected circuit must not prevent other users from receiving
            // the update or affect the workflow transaction that already succeeded.
        }
    }
}
