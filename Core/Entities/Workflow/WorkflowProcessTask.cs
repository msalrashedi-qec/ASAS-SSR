using Core.Entities.Account;

namespace Core.Entities.Workflow
{
    public class WorkflowProcessTask
    {
        public Guid Id { get; set; }
        public Guid WfProcessId { get; set; }
        public int WfTaskId { get; set; }
        //public string ResponsibleRoleId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SharedWithUserId { get; set; }
        public DateTime? ShareStartDate { get; set; }
        public DateTime? ShareEndDate { get; set; }
        public byte StatusId { get; set; }

        public WorkflowProcess WfProcess { get; set; } = null!;
        public WorkflowTask WfTask { get; set; } = null!;
        //public ApplicationRole ResponsibleRole { get; set; } = null!;
        public WorkflowStatus Status { get; set; } = null!;
        public ApplicationUser SharedWithUser { get; set; } = null!;
    }
}
