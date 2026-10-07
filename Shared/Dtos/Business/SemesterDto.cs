
namespace Shared.Dtos.Business
{
    public class SemesterDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? NameAr { get; set; }
        public Guid SchoolId { get; set; }
        public int YearId { get; set; }
        public byte SemesterId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
