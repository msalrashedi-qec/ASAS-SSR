
namespace Core.Entities.Business
{
    public class BusAssignment : GuidEntityBase
    {
        public int BusId { get; set; }
        public Guid DriverId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string SupervisorName { get; set; }
        public string SupervisorMobile { get; set; }

        public virtual Bus Bus { get; set; } = null!;
        public virtual BusDriver Driver { get; set; } = null!;
    }
}
