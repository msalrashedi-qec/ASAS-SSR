
using Core.Entities.Business;

namespace Core.Entities.General
{
    public class District : EntityBase
    {
        public string Name { get; set; }
        public string NameAr { get; set; }
        public int CityId { get; set; }

        public virtual City City { get; set; } = null!;

        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
