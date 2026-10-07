namespace Shared.Dtos.Workflow
{
    public class WorkflowAgingDto
    {
        public int TypeId { get; set; }
        public string? TypeName { get; set; }

        public int From0To7 { get; set; }
        public List<InboxDto> From0To7Tasks { get; set; } = new();

        public int From8To30 { get; set; }
        public List<InboxDto> From8To30Tasks { get; set; } = new();
        public int From31To60 { get; set; }
        public List<InboxDto> From31To60Tasks { get; set; } = new();

        public int Over60 { get; set; }
        public List<InboxDto> Over60Tasks { get; set; } = new();

        public string? RowStyle { get; set; }
    }
}
