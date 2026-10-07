using Core.Entities.General;

namespace Core.Entities.Business
{
    public class StudentContractDetail
    {
        public Guid Id { get; set; }
        public Guid ContractId { get; set; }
        public int ServiceId { get; set; }
        public byte SemesterId { get; set; }
        public double Amount { get; set; }
        public string? Comments { get; set; }

        public virtual StudentContract Contract { get; set; } = null!;
        public virtual Service Service { get; set; } = null!;
        public virtual Semester Semester { get; set; } = null!;

    }
}
