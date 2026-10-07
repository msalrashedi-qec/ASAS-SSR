namespace Core.Entities.Workflow
{
    public class WorkflowAction
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int WorkflowTypeId { get; set; }

        public virtual WorkflowType WorkflowType { get; set; } = null!;
        public virtual ICollection<WorkflowTask> WorkflowTasks { get; set; } = new HashSet<WorkflowTask>();
    }
}
