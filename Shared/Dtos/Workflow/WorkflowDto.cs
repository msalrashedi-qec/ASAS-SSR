namespace Shared.Dtos.Workflow
{
    public class WorkflowDto
    {
        public int Id { get; set; }
        public string Version { get; set; }
        public int TypeId { get; set; }
        public string? TypeName { get; set; }
        public bool IsActive { get; set; }
    }
}
