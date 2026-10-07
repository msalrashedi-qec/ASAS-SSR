
using Core.Entities.Discounts;
using Core.Entities.General;

namespace Core.Entities.Business
{
    public class School : GuidEntityBase
    {
        public int CompanyId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }
        public byte TypeId { get; set; }
        public int CityId { get; set; }
        public string Logo { get; set; }
        public string Address { get; set; }
        public string? PO { get; set; }
        public string? PoCode { get; set; }
        public string Email { get; set; }
        public string SmsSender { get; set; }
        public string SmsUserName { get; set; }
        public string SmsPassword { get; set; }

        public string ChequePayeeName { get; set; }
        public string BankName { get; set; }
        public string IBAN { get; set; }



        public virtual Company Company { get; set; } = null!;
        public virtual SchoolType Type { get; set; } = null!;
        public virtual City City { get; set; } = null!;

        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        public virtual ICollection<SchoolLevel> Levels { get; set; } = new List<SchoolLevel>();
        public virtual ICollection<SchoolSemester> SchoolSemesters { get; set; } = new List<SchoolSemester>();
        public virtual ICollection<Service> Services { get; set; } = new List<Service>();
        public virtual ICollection<ServiceVAT> ServiceVATs { get; set; } = new List<ServiceVAT>();
        public virtual ICollection<Bus> Buses { get; set; } = new List<Bus>();
        public virtual ICollection<BusDriver> BusDrivers { get; set; } = new List<BusDriver>();
        public virtual ICollection<DiscountDetail> DiscountDetails { get; set; } = new List<DiscountDetail>();

    }
}
