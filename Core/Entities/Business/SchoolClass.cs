
namespace Core.Entities.Business
{
    public class SchoolClass : EntityBase
    {
        public string Name { get; set; }
        public string NameAr { get; set; }
        public int LevelId { get; set; }
        public int SN { get; set; }
        public bool IsGraduationClass { get; set; }

        public virtual SchoolLevel Level { get; set; } = null!;

        public virtual ICollection<SchoolClassRoom> ClassRooms { get; set; } = new List<SchoolClassRoom>();
        public virtual ICollection<StudentContract> StudentContracts { get; set; } = new List<StudentContract>();
    }
}
