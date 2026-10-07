namespace Shared.Dtos.Workflow
{
    public class WfProcessSummaryDto
    {
        public string? RefNo { get; set; }
        public Guid? ProcessId { get; set; }
        public string TypeName { get; set; }
        public string StatusName { get; set; }
        public string ColorCode { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
