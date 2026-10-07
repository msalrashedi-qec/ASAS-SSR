using Core.Entities.Account;

namespace Core.Entities.Workflow
{
    public class WorkflowFutureSharing : GuidEntityBase
    {
        public int WorkflowTaskId { get; set; }
        public string SharedFromUserId { get; set; }
        public string SharedWithUserId { get; set; }
        public DateTime ShareStartDate { get; set; }
        public DateTime ShareEndDate { get; set; }

        public virtual WorkflowTask WorkflowTask { get; set; } = null!;

        public virtual ApplicationUser SharedFromUser { get; set; } = null!;
        public virtual ApplicationUser SharedWithUser { get; set; } = null!;
       
    }
}
