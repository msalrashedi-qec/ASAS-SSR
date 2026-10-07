using Core.Entities.Account;
using Core.Entities.Workflow;

namespace Core.Entities.General
{
    public class SystemAction
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string NameAr { get; set; } = null!;

        ////////////// Tables Relationship /////////////////
        public virtual ICollection<UserAudit> UserAudits { get; set; }
        public virtual ICollection<WorkflowHistory> WorkflowHistories { get; set; }
    }
}
