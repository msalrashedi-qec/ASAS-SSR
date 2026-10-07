
namespace Shared.Dtos.Workflow
{
    public class ShareTasksDto
    {
        public List<Guid> WfTasks { get; set; } = new();
        public List<int> FutureWfTasks { get; set; } = new();

        public string? SharedFromUserId { get; set; }
        public string? SharedWithUserId { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now;
        public string? Comments { get; set; }
    }
}
