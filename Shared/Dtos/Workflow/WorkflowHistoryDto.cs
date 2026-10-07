using Shared.Dtos.General;

namespace Shared.Dtos.Workflow
{
    public class WorkflowHistoryDto
    {
        public Guid Id { get; set; }
        public int ActionTypeId { get; set; }
        public string? ActionTypeName { get; set; }
        public string? ActionTypeNameAr { get; set; }
        public string? TaskName { get; set; }
        public string? TaskNameAr { get; set; }
        public string? Comments { get; set; }
        public string? UserName { get; set; }
        public DateTime CreatedOn { get; set; }
        public List<FileDto> Attachments { get; set; } = new();

    }
}
