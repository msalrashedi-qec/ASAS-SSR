namespace Web.Services
{
    public enum StudentParentChangeType
    {
        ParentCreated,
        ParentUpdated,
        StudentCreated,
        StudentUpdated
    }

    public sealed record StudentParentChangedEvent(StudentParentChangeType ChangeType, Guid? SchoolId = null, Guid? ParentId = null, Guid? StudentId = null);

    public class StudentParentChangeNotificationService
    {
        public event Func<StudentParentChangedEvent, Task>? Changed;

        public async Task NotifyAsync(StudentParentChangedEvent change)
        {
            var handlers = Changed?.GetInvocationList().Cast<Func<StudentParentChangedEvent, Task>>().ToArray();

            if (handlers is null || handlers.Length == 0)
                return;

            await Task.WhenAll(handlers.Select(handler => NotifySubscriberAsync(handler, change)));
        }

        private static async Task NotifySubscriberAsync(Func<StudentParentChangedEvent, Task> handler, StudentParentChangedEvent change)
        {
            try
            {
                await handler(change);
            }
            catch
            {
                // Ignore stale/disposed circuits so one subscriber cannot block a successful save.
            }
        }
    }
}
