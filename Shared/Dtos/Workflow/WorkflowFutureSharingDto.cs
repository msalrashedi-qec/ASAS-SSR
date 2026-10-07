namespace Shared.Dtos.Workflow
{
    public class WorkflowFutureSharingDto
    {
        public Guid Id { get; set; }
        public bool IsSelected { get; set; }
        public int WfTaskId { get; set; }
        public string? TaskName { get; set; }
        public string? TypeName { get; set; }
        public string? WfVersion { get; set; }
        public string? SharedWithName { get; set; }
        public DateTime? ShareStartDate { get; set; }
        public DateTime? ShareEndDate { get; set; }
        public string? Comments { get; set; }
    }
}
