using Shared.Dtos.Workflow;

namespace Web.Components.Partial.WF
{
    public partial class WfHistoryViewer
    {
        [Parameter]
        public IEnumerable<WorkflowHistoryDto>? Histories { get; set; }

    }
}