using Core.Entities.Account;
using Core.Entities.General;

namespace Core.Entities.Workflow
{
    public class WorkflowHistory
    {
        public Guid Id { get; set; }
        public Guid WfProcessId { get; set; }
        public int ActionId { get; set; }
        public string? Comments { get; set; }
        public int TaskId { get; set; }
        public string CreatorId { get; set; }
        public DateTime CreatedOn { get; set; }

        public virtual WorkflowProcess WfProcess { get; set; } = null!;
        public virtual SystemAction Action { get; set; } = null!;
        public virtual WorkflowTask Task { get; set; } = null!;
        public virtual ApplicationUser Creator { get; set; } = null!;
        public virtual ICollection<WorkflowAttachment> Attachments { get; set; } = new HashSet<WorkflowAttachment>();

    }
}
