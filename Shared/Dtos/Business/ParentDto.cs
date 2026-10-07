
namespace Shared.Dtos.Business
{
    public class ParentDto
    {
        public Guid Id { get; set; }
        public byte GenderId { get; set; }
        public string? Title { get; set; }
        public string? TitleAr { get; set; }
        public string Name { get; set; }
        public string NationalID { get; set; }
        public int NationalityId { get; set; }
        public string? NationalityName { get; set; }
        public string? NationalityNameAr { get; set; }
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
        public string? Comments { get; set; }

        public List<StudentDto> Students { get; set; } = new();
    }
}
