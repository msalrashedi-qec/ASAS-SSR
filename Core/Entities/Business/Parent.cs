using Core.Entities.General;

namespace Core.Entities.Business
{
    public class Parent : GuidEntityBase
    {
        public string Name { get; set; }
        public byte GenderId { get; set; }
        public string NationalID { get; set; }
        public int NationalityId { get; set; }
        public string NationalIDIssuedFrom { get; set; }
        public DateTime NationalIDExpiryDate { get; set; }
        public string? Occupation { get; set; }
        public string Mobile { get; set; }
        public string? Email { get; set; }
        public string? HomeAddress { get; set; }
        public string? HomePhone { get; set; }
        public string? WorkAddress { get; set; }
        public string? WorkPhone { get; set; }
        public string? PO { get; set; }
        public string? PoCode { get; set; }

        public virtual Gender Gender { get; set; } = null!;
        public virtual Nationality Nationality { get; set; } = null!;

        public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    }
}
