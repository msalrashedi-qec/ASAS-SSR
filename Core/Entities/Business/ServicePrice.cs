using Core.Entities.General;

namespace Core.Entities.Business
{
    public class ServicePrice : GuidEntityBase
    {
        public int YearId { get; set; }
        public int ServiceId { get; set; }
        public int ClassId { get; set; }
        public byte SemesterId { get; set; }
        public double Price { get; set; }

        public virtual Year Year { get; set; } = null!;
        public virtual SchoolClass Class { get; set; } = null!;
        public virtual Service Service { get; set; } = null!;
        public virtual Semester Semester { get; set; } = null!;
    }
}
