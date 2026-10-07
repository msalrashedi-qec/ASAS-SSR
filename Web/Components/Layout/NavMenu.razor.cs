using Microsoft.JSInterop;

namespace Web.Components.Layout
{
    public partial class NavMenu : IDisposable
    {
        [Inject] private WorkflowUpdateNotifier WorkflowUpdateNotifier { get; set; } = default!;

        private readonly SemaphoreSlim _countRefreshLock = new(1, 1);
        private int? _incomingRequestCount;
        private bool _disposed;

        private string FormattedIncomingRequestCount => _incomingRequestCount > 99
            ? "99+"
            : _incomingRequestCount.GetValueOrDefault().ToString(CultureInfo.InvariantCulture);

        protected override async Task OnInitializedAsync()
        {
            WorkflowUpdateNotifier.Updated += OnWorkflowUpdatedAsync;
            await RefreshIncomingRequestCountAsync();
        }

        private Task OnWorkflowUpdatedAsync(WorkflowUpdate update)
        {
            if (_disposed)
                return Task.CompletedTask;

            return InvokeAsync(async () =>
            {
                await RefreshIncomingRequestCountAsync();
                StateHasChanged();
            });
        }

        private async Task RefreshIncomingRequestCountAsync()
        {
            await _countRefreshLock.WaitAsync();
            try
            {
                if (_disposed) return;

                var userId = await _userService.GetUserIdAsync();
                var userRoles = await _uow.UserRoles.FindAllAsync(x => x.UserId == userId);
                var roleIds = userRoles.Select(x => x.RoleId).ToList();

                _incomingRequestCount = roleIds.Count == 0 ? 0 : await _uow.WorkflowProcessTasks.CountAsync(x => roleIds.Contains(x.WfTask.ResponsibleRoleId) && x.StatusId == 2);
            }
            finally
            {
                _countRefreshLock.Release();
            }
        }

        private async Task CloseModal()
        {
            await _js.InvokeVoidAsync("APP.closeMenu");
        }

        public void Dispose()
        {
            _disposed = true;
            WorkflowUpdateNotifier.Updated -= OnWorkflowUpdatedAsync;
        }
    }
}