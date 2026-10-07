
namespace Core.Entities.Workflow
{
    public class WorkflowStatus
    {
        public byte Id { get; set; }
        public string Name { get; set; } = null!;
        public string NameAr { get; set; } = null!;
        public string ColorCode { get; set; } = null!;

        public virtual ICollection<WorkflowProcess> WorkflowProcesses { get; set; } = new HashSet<WorkflowProcess>();
        public virtual ICollection<WorkflowProcessTask> WorkflowProcessTasks { get; set; } = new HashSet<WorkflowProcessTask>();
    }
}
