
namespace Core.Entities.Workflow
{
    public class WorkflowAttachment
    {
        public Guid Id { get; set; }
        public Guid HistoryId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }

        public virtual WorkflowHistory History { get; set; } = null!;
    }
}
