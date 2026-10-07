

using Core.Entities.Workflow;

namespace Core.Entities.Account
{
    public class RoleWorkflowType
    {
        public Guid Id { get; set; }
        public string RoleId { get; set; }
        public int WorkflowTypeId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }

        public WorkflowType WorkflowType { get; set; } = null!;
    }
}
