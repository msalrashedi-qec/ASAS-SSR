using Core.Entities.Business;
using Core.Entities.Discounts;

namespace Core.Entities.General
{
    public class Semester
    {
        public byte Id { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }

        public virtual ICollection<SchoolSemester> SchoolSemesters { get; set; } = new List<SchoolSemester>();
        public virtual ICollection<ServicePrice> ServicePrices { get; set; } = new List<ServicePrice>();
        public virtual ICollection<ReceiptDetail> ReceiptDetails { get; set; } = new List<ReceiptDetail>();
        public virtual ICollection<RefundReceiptDetail> RefundReceiptDetails { get; set; } = new List<RefundReceiptDetail>();
        public virtual ICollection<StudentAccount> StudentAccounts { get; set; } = new List<StudentAccount>();
        public virtual ICollection<StudentContractDetail> ContractDetails { get; set; } = new List<StudentContractDetail>();
        public virtual ICollection<DiscountDetail> DiscountDetails { get; set; } = new List<DiscountDetail>();
        public virtual ICollection<StudentSubscription> StudentSubscriptions { get; set; } = new List<StudentSubscription>();
        public virtual ICollection<DiscountRequestDetail> StudentRequestSemesters { get; set; } = new List<DiscountRequestDetail>();

    }
}
