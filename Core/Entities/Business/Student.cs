using Core.Entities.Discounts;
using Core.Entities.General;
using Core.Entities.Workflow;

namespace Core.Entities.Business
{
    public class Student : GuidEntityBase
    {
        public int SN { get; set; }
        public string Name { get; set; }
        public string FatherName { get; set; }
        public Guid SchoolId { get; set; }
        public Guid ParentId { get; set; }
        public byte GenderId { get; set; }
        public string NationalID { get; set; }
        public int NationalityId { get; set; }
        public DateTime BirthDate { get; set; }
        public int DistrictId { get; set; }
        public string? HomeAddress { get; set; }
        public string? HomePhone { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactMobile { get; set; }
        public Guid CurrentContractId { get; set; }
        public byte CurrentStatusId { get; set; }

        public virtual School School { get; set; } = null!;
        public virtual Parent Parent { get; set; } = null!;
        public virtual Gender Gender { get; set; } = null!;
        public virtual Nationality Nationality { get; set; } = null!;
        public virtual District District { get; set; } = null!;
        public virtual StudentContract CurrentContract { get; set; } = null!;
        public virtual StudentStatus CurrentStatus { get; set; } = null!;

        public virtual ICollection<StudentDocument> Documents { get; set; } = new List<StudentDocument>();
        public virtual ICollection<StudentContract> Contracts { get; set; } = new List<StudentContract>();
        public virtual ICollection<DiscountRequest> Requests { get; set; } = new List<DiscountRequest>();
        public virtual ICollection<WorkflowProcess> WfProcess { get; set; } = new List<WorkflowProcess>();
    }
}
