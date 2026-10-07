namespace Shared.Dtos.Workflow
{
    public class WorkflowTaskDto
    {
        public int Id { get; set; }
        public int WorkflowId { get; set; }
        public int SN { get; set; }
        public string Name { get; set; }
        public string ResponsibleRoleId { get; set; }
        public string? RoleName { get; set; }
        public int RequiredDays { get; set; }
        public Guid? SharedWith { get; set; }
        public string? UserName { get; set; }
        public DateTime? ShareStartDate { get; set; }
        public DateTime? ShareEndDate { get; set; }
        public int ActionId { get; set; }
        public int JumpToTaskId { get; set; }
        public string? ActionName { get; set; }
        public bool IsTheEnd { get; set; } = false;
        public bool AllowWithdrawal { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSelectable { get; set; }
        public bool IsSelected { get; set; }
    }
}
