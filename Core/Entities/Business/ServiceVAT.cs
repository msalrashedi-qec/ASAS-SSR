using Core.Entities.General;

namespace Core.Entities.Business
{
    public class ServiceVAT : GuidEntityBase
    {
        public Guid SchoolId { get; set; }
        public int YearId { get; set; }
        public int ServiceId { get; set; }
        public int NationalityId { get; set; }
        public decimal VAT { get; set; }


        public virtual School School { get; set; } = null!;
        public virtual Year Year { get; set; } = null!;
        public virtual Service Service { get; set; } = null!;
        public virtual Nationality Nationality { get; set; } = null!;
    }
}
