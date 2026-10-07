namespace Shared.Dtos.Workflow
{
    public class WorkflowFilterDto
    {
        public string? UserId { get; set; }
        public string? RefNo { get; set; }
        public int TypeId { get; set; }
        public IList<byte>? Statuses { get; set; } = [2];
        public IList<int>? Types { get; set; }
        public IList<Guid>? Projects { get; set; }
        public byte SortColumn { get; set; } = 100;
        public string OrderBy { get; set; } = "DESC";
        public string Language { get; set; } = "en";
    }
}
