
namespace Shared.Dtos.Business
{
    public class SchoolClassDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }
        public int SN { get; set; }
        public bool IsGraduationClass { get; set; }
        public int LevelId { get; set; }
        public string? LevelName { get; set; }
        public string? LevelNameAr { get; set; }
        public string? LevelSN { get; set; }
        public Guid SchoolId { get; set; }
        public string? SchoolName { get; set; }
        public string? SchoolNameAr { get; set; }
    }
}
