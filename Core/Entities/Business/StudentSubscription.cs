using Core.Entities.General;

namespace Core.Entities.Business
{
    public class StudentSubscription : GuidEntityBase
    {
        public Guid ContractId { get; set; }
        public int ServiceId { get; set; }
        public byte SemesterId { get; set; }
        public DateTime SubscriptionDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsPaid { get; set; }
        

        public virtual StudentContract Contract { get; set; } = null!;
        public virtual Service Service { get; set; } = null!;
        public virtual Semester Semester { get; set; } = null!;
        public virtual ICollection<StudentBusAssignment> BusAssignments { get; set; } = new List<StudentBusAssignment>();
    }
}
