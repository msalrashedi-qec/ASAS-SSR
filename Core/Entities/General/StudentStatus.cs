
using Core.Entities.Business;

namespace Core.Entities.General
{
    public class StudentStatus
    {
        public byte Id { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }
        public string ColorCode { get; set; }

        public virtual ICollection<StudentContract> StudentContracts { get; set; } = new List<StudentContract>();
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
