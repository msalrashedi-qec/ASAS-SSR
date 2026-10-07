
using Core.Entities.Account;

namespace Core.Entities.Workflow
{
    public class WorkflowType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
        public int SN { get; set; }

        public virtual ICollection<Workflow> Workflows { get; set; } = new HashSet<Workflow>();
        public virtual ICollection<RoleWorkflowType> RoleWorkflowTypes { get; set; } = new HashSet<RoleWorkflowType>();
        public virtual ICollection<WorkflowAction> WorkflowActions { get; set; } = new HashSet<WorkflowAction>();
    }
}
