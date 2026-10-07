
using Core.Entities.Business;

namespace Core.Entities.General
{
    public class City : EntityBase
    {
        public string Name { get; set; }
        public string NameAr { get; set; }

        public virtual ICollection<District> Districts { get; set; } = new List<District>();
        public virtual ICollection<School> Schools { get; set; } = new List<School>();
    }
}
