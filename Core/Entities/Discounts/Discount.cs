using Core.Entities.General;

namespace Core.Entities.Discounts
{
    public class Discount: EntityBase
    {
        public int SN { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }
        public byte TypeId { get; set; }
        public int TransactionTypeId { get; set; }
        public int ServiceCategoryId { get; set; }
        public byte DependencyId { get; set; }
        public Shared.Enums.DiscountApplicationTiming ApplicationTiming { get; set; } = Shared.Enums.DiscountApplicationTiming.OnSubscription;
        public bool IsShownInContract { get; set; }
        public bool IsCancelledOnWithdrawal { get; set; }

        public virtual DiscountType Type { get; set; } = null!;
        public virtual TransactionType TransactionType { get; set; } = null!;
        public virtual ServiceCategory ServiceCategory { get; set; } = null!;
        public virtual DiscountDependency Dependency { get; set; } = null!;

        public virtual ICollection<DiscountDetail> Details { get; set; } = new List<DiscountDetail>();

    }
}
