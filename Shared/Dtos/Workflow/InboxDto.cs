using Shared.Dtos.General;

namespace Shared.Dtos.Workflow
{
    public class InboxDto
    {
        public Guid Id { get; set; }
        public string? RefNo { get; set; }
        public Guid WfProcessId { get; set; }
        public string? TypeName { get; set; }
        public string? TypeNameAr { get; set; }
        public string? ClassName { get; set; }
        public string? TaskName { get; set; }
        public string? TaskNameAr { get; set; }
        public List<BasicDto> Responsibles { get; set; } = new();
        public string? RequesterName { get; set; }
        public string? SchoolName { get; set; }
        public string? SchoolNameAr { get; set; }
        public byte StatusId { get; set; }
        public string? StatusName { get; set; }
        public string? StatusNameAr { get; set; }
        public string? ColorCode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RequiredDays { get; set; }
        public bool IsSelected { get; set; }

        public string? SharedWithName { get; set; }
        public DateTime? ShareStartDate { get; set; }
        public DateTime? ShareEndDate { get; set; }

        public List<BasicDto> CurrentTasks { get; set; } = new();
    }
}
