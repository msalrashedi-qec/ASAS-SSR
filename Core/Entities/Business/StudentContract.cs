using Core.Entities.General;

namespace Core.Entities.Business
{
    public class StudentContract : GuidEntityBase
    {
        public Guid StudentId { get; set; }
        public string SN { get; set; }
        public int YearId { get; set; }
        public int RegisteredForYearId { get; set; }
        public int ClassId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        public byte StatusId { get; set; }

        public virtual Student Student { get; set; } = null!;
        public virtual Year Year { get; set; } = null!;
        public virtual SchoolClass Class { get; set; } = null!;
        public virtual StudentStatus Status { get; set; } = null!;

        public virtual ICollection<StudentContractDetail> Details { get; set; } = new List<StudentContractDetail>();
        public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
        public virtual ICollection<RefundReceipt> RefundReceipts { get; set; } = new List<RefundReceipt>();
        public virtual ICollection<StudentAccount> StudentAccounts { get; set; } = new List<StudentAccount>();
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        public virtual ICollection<StudentSubscription> Subscriptions { get; set; } = new List<StudentSubscription>();
        public virtual ICollection<StudentClassRoomAssignment> ClassRoomAssignments { get; set; } = new List<StudentClassRoomAssignment>();
    }
}
