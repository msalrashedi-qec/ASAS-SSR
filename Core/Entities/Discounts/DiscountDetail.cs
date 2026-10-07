using Core.Entities.Business;
using Core.Entities.General;

namespace Core.Entities.Discounts
{
    public class DiscountDetail : GuidEntityBase
    {
        public int DiscountId { get; set; }
        public Guid SchoolId { get; set; }
        public int YearId { get; set; }
        public byte SemesterId { get; set; }
        public string FromRange { get; set; }
        public string ToRange { get; set; }
        public double Amount { get; set; }
        public double AdditionalFixedAmount { get; set; }

        public virtual Discount Discount { get; set; } = null!;
        public virtual School School { get; set; } = null!;
        public virtual Year Year { get; set; } = null!;
        public virtual Semester Semester { get; set; } = null!;

    }
}
