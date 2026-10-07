using Core.Entities.General;

namespace Core.Entities.Business
{
    public class RefundReceiptDetail : GuidEntityBase
    {
        public Guid ReceiptId { get; set; }
        public int ServiceId { get; set; }
        public int CategoryId { get; set; }
        public byte SemesterId { get; set; }
        public double Amount { get; set; }


        public virtual RefundReceipt Receipt { get; set; } = null!;
        public virtual Service Service { get; set; } = null!;
        public virtual ServiceCategory Category { get; set; } = null!;
        public virtual Semester Semester { get; set; } = null!;
    }
}
