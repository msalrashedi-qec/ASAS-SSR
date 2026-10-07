
namespace Shared.Dtos.Workflow
{
    public class RequesterDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeContractId { get; set; }
        public string? FullName { get; set; }
        public string? NationalID { get; set; }
        public int OuId { get; set; }
        public string? OuName { get; set; }
        public string? PositionName { get; set; }
        public string? SponsorName { get; set; }
        public string? NationalityName { get; set; }
        public int AirportId { get; set; }
        public string? AirportName { get; set; }
        public string? ContractPeriod { get; set; }
        public byte StatusId { get; set; } = 0;
        public string? StatusName { get; set; }
        public string? ColorCode { get; set; }
        public double NetSalary { get; set; } = 0;
    }
}
