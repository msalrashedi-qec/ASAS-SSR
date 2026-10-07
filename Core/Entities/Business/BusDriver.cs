using Core.Entities.General;

namespace Core.Entities.Business
{
    public class BusDriver : GuidEntityBase
    {
        public string Name { get; set; }
        public string NationalID { get; set; }
        public string Mobile { get; set; }
        public int NationalityId { get; set; }
        public Guid SchoolId { get; set; }

        public virtual Nationality Nationality { get; set; } = null!;
        public virtual School School { get; set; } = null!;
        public virtual ICollection<BusAssignment> BusAssignments { get; set; } = new List<BusAssignment>();
    }
}
