using Core.Entities.General;

namespace Core.Entities.Business
{
    public class Service : EntityBase
    {
        public string Name { get; set; }
        public string NameAr { get; set; }
        public Guid SchoolId { get; set; }
        public int CategoryId { get; set; }
        public int YearId { get; set; }
        public decimal DefaultVAT { get; set; }


        public virtual School School { get; set; } = null!;
        public virtual ServiceCategory Category { get; set; } = null!;
        public virtual Year Year { get; set; } = null!;

        public virtual ICollection<ServicePrice> ServicePrices { get; set; } = new List<ServicePrice>();
        public virtual ICollection<ServiceVAT> ServiceVATs { get; set; } = new List<ServiceVAT>();
        public virtual ICollection<ReceiptDetail> ReceiptDetails { get; set; } = new List<ReceiptDetail>();
        public virtual ICollection<RefundReceiptDetail> RefundReceiptDetails { get; set; } = new List<RefundReceiptDetail>();
        public virtual ICollection<StudentAccount> StudentAccounts { get; set; } = new List<StudentAccount>();
        public virtual ICollection<StudentContractDetail> ContractDetails { get; set; } = new List<StudentContractDetail>();
        public virtual ICollection<StudentSubscription> StudentSubscriptions { get; set; } = new List<StudentSubscription>();

    }
}
