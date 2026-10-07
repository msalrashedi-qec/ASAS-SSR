
using Core.Entities.Business;
using Core.Entities.Discounts;

namespace Core.Entities.General
{
    public class ServiceCategory: EntityBase
    {
        public string Name { get; set; }
        public string NameAr { get; set; }

        public virtual ICollection<Service> Services { get; set; } = new List<Service>();
        public virtual ICollection<ReceiptDetail> ReceiptDetails { get; set; } = new List<ReceiptDetail>();
        public virtual ICollection<RefundReceiptDetail> RefundReceiptDetails { get; set; } = new List<RefundReceiptDetail>();
        public virtual ICollection<StudentAccount> StudentAccounts { get; set; } = new List<StudentAccount>();
        public virtual ICollection<Discount> Discounts { get; set; } = new List<Discount>();
    }
}
