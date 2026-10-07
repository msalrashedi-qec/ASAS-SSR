using Core.Entities.General;

namespace Core.Entities.Business
{
    public class StudentAccount : GuidEntityBase
    {
        public Guid ContractId { get; set; }
        public byte SemesterId { get; set; }
        public int ServiceId { get; set; }
        public int Categoryid { get; set; }
        public int TransactionTypeId { get; set; }
        public string? RefNo { get; set; }
        public double Debit { get; set; }
        public double Credit { get; set; }
        public decimal Percentage { get; set; }
        public DateTime Date { get; set; }

        public virtual StudentContract Contract { get; set; } = null!;
        public virtual Semester Semester { get; set; } = null!;
        public virtual Service Service { get; set; } = null!;
        public virtual ServiceCategory Category { get; set; } = null!;
        public virtual TransactionType TransactionType { get; set; } = null!;

    }
}
