namespace Core.Entities.Workflow
{
    public class WorkflowPermission
    {
        public Guid Id { get; set; }
        public int WorkflowTaskId { get; set; }
        public int PermissionId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set;}

        public virtual WorkflowTask WorkflowTask { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
    }
}
