using Core.Entities.Business;
using Core.Entities.Discounts;

namespace Core.Entities.General
{
    public class Year : EntityBase
    {
        public string Name { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsNext { get; set; }

        public virtual ICollection<StudentDocument> StudentDocuments { get; set; } = new List<StudentDocument>();
        public virtual ICollection<SchoolSemester> Semesters { get; set; } = new List<SchoolSemester>();
        public virtual ICollection<Service> Services { get; set; } = new List<Service>();
        public virtual ICollection<ServicePrice> ServicePrices { get; set; } = new List<ServicePrice>();
        public virtual ICollection<StudentContract> StudentContracts { get; set; } = new List<StudentContract>();
        public virtual ICollection<DiscountDetail> DiscountDetails { get; set; } = new List<DiscountDetail>();
        public virtual ICollection<ServiceVAT> ServiceVATs { get; set; } = new List<ServiceVAT>();
    }
}
