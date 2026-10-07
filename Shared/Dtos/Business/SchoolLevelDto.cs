
namespace Shared.Dtos.Business
{
    public class SchoolLevelDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }
        public int SN { get; set; }
        public Guid SchoolId { get; set; }
        public string? SchoolCode { get; set; }
        public string? SchoolName { get; set; }
        public string? SchoolNameAr { get; set; }
        
    }
}
