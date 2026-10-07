using Core.Entities.Workflow;
using Core.Entities.Business;

namespace Core.Entities.Discounts;

public class DiscountRequest : GuidEntityBase
{
    public Guid StudentId { get; set; }
    public Guid WfProcessId { get; set; }
    public int DiscountId { get; set; }
   

    public virtual Student Student { get; set; } = null!;
    public virtual WorkflowProcess WfProcess { get; set; } = null!;
    public virtual Discount? Discount { get; set; }
    
    public virtual ICollection<DiscountRequestDetail> Details { get; set; } = new List<DiscountRequestDetail>();
}
