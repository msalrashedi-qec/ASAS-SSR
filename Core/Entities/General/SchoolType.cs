using Core.Entities.Business;

namespace Core.Entities.General
{
    public class SchoolType
    {
        public byte Id { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }

        public virtual ICollection<School> Schools { get; set; } = new List<School>();
    }
}
