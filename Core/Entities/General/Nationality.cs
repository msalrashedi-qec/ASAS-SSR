using Core.Entities.Business;

namespace Core.Entities.General
{
    public class Nationality :EntityBase
    {
        public string Name { get; set; }
        public string NameAr { get; set; }

        public virtual ICollection<Parent> Parents { get; set; } = new List<Parent>();
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        public virtual ICollection<ServiceVAT> ServiceVATs { get; set; } = new List<ServiceVAT>();
        public virtual ICollection<BusDriver> BusDrivers { get; set; } = new List<BusDriver>();
    }
}
