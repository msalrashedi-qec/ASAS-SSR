namespace Core.Entities.Workflow
{
    public class Permission
    {
        public int Id { get; set; }
        public string Name { get; set; }
       

        public virtual ICollection<WorkflowPermission> WorkflowPermissions { get; set; } = new HashSet<WorkflowPermission>();
    }
}
