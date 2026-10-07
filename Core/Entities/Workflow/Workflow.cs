
namespace Core.Entities.Workflow
{
    public class Workflow : EntityBase
    {
        public string Version { get; set; } = null!;
        public int TypeId { get; set; }
        public bool IsActive { get; set; }
        public WorkflowType Type { get; set; } = null!;
        public virtual ICollection<WorkflowTask> WorkflowTasks { get; set; } = new HashSet<WorkflowTask>();
        public virtual ICollection<WorkflowProcess> WorkflowProcesses { get; set; } = new HashSet<WorkflowProcess>();


    }
}
