
using Shared.Dtos.General;

namespace Shared.Dtos.Workflow
{
    public class WorkflowProcessDto
    {
        public Guid? TaskId { get; set; }
        public Guid? ProcessId { get; set; }
        public int WorkflowId { get; set; }

        public int CurrentTaskId { get; set; }
        public int CurrentTaskSN { get; set; }
        public string? CurrentTaskName { get; set; }
        public string? CurrentTaskNameAr { get; set; }
        public int WorkflowTypeId { get; set; }
        public string? RefNo { get; set; }
        public int ActionId { get; set; }
        public Guid RequestedForId { get; set; }
        public string? FileNo { get; set; }
        public string? EmployeeName { get; set; }
        public byte StatusId { get; set; }
        public string? StatusName { get; set; }
        public string? StatusNameAr { get; set; }
        public string? ColorCode { get; set; }
        public string? Comments { get; set; }
        public bool AllowWithdrawal { get; set; }


        public IEnumerable<WorkflowHistoryDto> Histories { get; set; } = new List<WorkflowHistoryDto>();
        public IEnumerable<BasicDto> Permissions { get; set; } = new List<BasicDto>();
    }
}
