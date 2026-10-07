namespace Shared.Dtos.General
{
    public class DashboardDto
    {
        public int ServiceDays { get; set; } = 0;
        public string LeaveBalance { get; set; } = "0";
        public int OpenRequestsCount { get; set; } = 0;
        public double Loans { get; set; } = 0;
        //public List<WfProcessSummaryDto> OpenRequests { get; set; } = new();
    }
}
