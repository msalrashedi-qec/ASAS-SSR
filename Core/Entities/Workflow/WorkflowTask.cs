using Core.Entities.Account;

namespace Core.Entities.Workflow
{
    public class WorkflowTask : EntityBase
    {
        public int WorkflowId { get; set; }
        public int SN { get; set; }
        public string Name { get; set; }
        public string ResponsibleRoleId { get; set; }
        public int RequiredDays { get; set; }
        
        public int ActionId { get; set; }
        public int JumpToTaskId { get; set; }
        public bool IsTheEnd { get; set; } = false;
        public bool AllowWithdrawal { get; set; }

        public virtual Workflow? Workflow { get; set; } = null!;
        public ApplicationRole ResponsibleRole { get; set; } = null!;
        public virtual WorkflowAction Action { get; set; } = null!;
        //public virtual ICollection<WorkflowProcess> WorkflowProcesses { get; set; } = new HashSet<WorkflowProcess>();
        public virtual ICollection<WorkflowHistory> Histories { get; set; } = new HashSet<WorkflowHistory>();
        public virtual ICollection<WorkflowProcessTask> WorkflowProcessTasks { get; set; } = new HashSet<WorkflowProcessTask>();
        public virtual ICollection<WorkflowFutureSharing> WorkflowFutureSharings { get; set; } = new HashSet<WorkflowFutureSharing>();
        public virtual ICollection<WorkflowPermission> WorkflowPermissions { get; set; } = new HashSet<WorkflowPermission>();
    }
}
