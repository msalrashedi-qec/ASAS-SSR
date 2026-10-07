
using Core.Entities.General;

namespace Core.Entities.Business
{
    public class Bus : EntityBase
    {
        public string Name { get; set; }
        public string PlateNo { get; set; }
        public byte TypeId { get; set; }
        public int ManufacturingYear { get; set; }
        public string Color { get; set; }
        public string VIN { get; set; }
        public int Capacity { get; set; }
        public string RegisterNo { get; set; }
        public DateTime RegistrationExpiry { get; set; }
        public Guid SchoolId { get; set; }

        public virtual BusType Type { get; set; } = null!;
        public virtual School School { get; set; } = null!;

        public virtual ICollection<BusAssignment> BusAssignments { get; set; } = new List<BusAssignment>();
        public virtual ICollection<StudentBusAssignment> StudentAssignments { get; set; } = new List<StudentBusAssignment>();

    }
}
