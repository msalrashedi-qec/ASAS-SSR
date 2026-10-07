
using Core.Entities.Business;

namespace Core.Entities.General
{
    public class Gender
    {
        public byte Id { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }
        public string Title { get; set; }
        public string TitleAr { get; set; }

        public virtual ICollection<Parent> Parents { get; set; } = new List<Parent>();
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        public virtual ICollection<SchoolClassRoom> ClassRooms { get; set; } = new List<SchoolClassRoom>();
    }
}
