using Core.Entities.Account;
using Core.Entities.General;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities.Business
{
    public class Receipt :GuidEntityBase
    {
        public int SN { get; set; }
        public Guid ContractId { get; set; }
        public DateTime Date { get; set; }
        public byte PaymentMethodId { get; set; }
        public string? ChequeNo { get; set; }
        public string? BankName { get; set; }
        public DateTime? ChequeDate { get; set; }

       
        public virtual StudentContract Contract { get; set; } = null!;
        public virtual PaymentMethod PaymentMethod { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        public virtual ApplicationUser User { get; set; } = null!;

        public virtual ICollection<ReceiptDetail> ReceiptDetails { get; set; } = new List<ReceiptDetail>();
    }
}
