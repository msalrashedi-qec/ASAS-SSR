using Core.Entities.Business;

namespace Core.Entities.General
{
    public class PaymentMethod
    {
        public byte Id { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }

        public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
        public virtual ICollection<RefundReceipt> RefundReceipts { get; set; } = new List<RefundReceipt>();
    }
}
