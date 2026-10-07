using Core.Entities.General;

namespace Core.Entities.Business
{
    public class SchoolSemester : GuidEntityBase
    {
        public Guid SchoolId { get; set; }
        public int YearId { get; set; }
        public byte SemesterId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public virtual School School { get; set; } = null!;
        public virtual Year Year { get; set; } = null!;
        public virtual Semester Semester { get; set; } = null!;
    }
}
