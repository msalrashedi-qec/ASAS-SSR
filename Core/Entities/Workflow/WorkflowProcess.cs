using Core.Entities.Business;
using Core.Entities.Discounts;

namespace Core.Entities.Workflow
{
    public class WorkflowProcess : GuidEntityBase
    {
        public int WorkflowId { get; set; }
        public string RefNo { get; set; }
        public Guid RequestedForId { get; set; }
        public byte StatusId { get; set; }
        public bool CanWithdraw { get; set; }

        public virtual Workflow Workflow { get; set; } = null!;
        public virtual Student RequestedFor { get; set; } = null!;
        public virtual WorkflowStatus Status { get; set; } = null!;

        public virtual ICollection<DiscountRequest> DiscountRequests { get; set; } = new HashSet<DiscountRequest>();
        public virtual ICollection<WorkflowHistory> WorkflowHistories { get; set; } = new HashSet<WorkflowHistory>();
        public virtual ICollection<WorkflowProcessTask> WorkflowProcessTasks { get; set; } = new HashSet<WorkflowProcessTask>();
        
    }
}
