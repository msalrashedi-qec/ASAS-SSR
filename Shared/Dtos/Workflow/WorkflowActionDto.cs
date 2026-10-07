
using Shared.Dtos.General;

namespace Shared.Dtos.Workflow
{
    public class WorkflowActionDto
    {
        public Guid? ProcessId { get; set; }
        public Guid TaskId { get; set; }
        public string? Comments { get; set; }
        public Guid? ReturnTaskId { get; set; }
        public bool IsAccountabilityAcceptedByIS { get; set; }
        public List<FileDto> Attachments { get; set; } = new();
    }
}
