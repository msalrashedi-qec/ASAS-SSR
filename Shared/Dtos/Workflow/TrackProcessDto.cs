
using Shared.Dtos.General;

namespace Shared.Dtos.Workflow
{
    public class TrackProcessDto
    {
        public int TaskSN { get; set; }
        public string? TaskName { get; set; }
        public string ResponsibleRoleId { get; set; }
        public string? RoleName { get; set; }
        public List<BasicDto> Responsibles { get; set; } = new();
        public byte StatusId { get; set; }
        public string? StatusName { get; set; }
        public string? ColorCode { get; set; }
        public DateTime? CompleteDate { get; set; }
    }
}
