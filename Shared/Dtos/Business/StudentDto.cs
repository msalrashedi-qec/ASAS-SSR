
namespace Shared.Dtos.Business
{
    public class StudentDto
    {
        public Guid Id { get; set; }
        public string? Code { get; set; }
        public string Name { get; set; }
        public string FatherName { get; set; }
        public Guid SchoolId { get; set; }
        public string? SchoolName { get; set; }
        public string? SchoolNameAr { get; set; }
        public Guid ParentId { get; set; }
        public string? ParentName { get; set; }
        public string? ParentMobile { get; set; }
        public string? ParentNationalityId { get; set; }
        public string? ParentNationalID { get; set; }
        public string? ParentAddress { get; set; }
        public byte GenderId { get; set; }
        public string NationalID { get; set; }
        public int NationalityId { get; set; }
        public string? NationalityName { get; set; }
        public string? NationalityNameAr { get; set; }
        public DateTime BirthDate { get; set; }
        public int DistrictId { get; set; }
        public string? HomeAddress { get; set; }
        public string? HomePhone { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactMobile { get; set; }
        public string? Comments { get; set; }

        public DateTime? RegistrationDate { get; set; }
        public int YearId { get; set; }
        public int RegisteredForYearId { get; set; }
        public Guid CurrentContractId { get; set; }
        public byte StatusId { get; set; }
        public string? StatusName { get; set; }
        public string? StatusNameAr { get; set; }
        public string? ColorCode { get; set; }

        public int ClassId { get; set; }
        public int ClassSN { get; set; }
        public string? ClassName { get; set; }
        public string? ClassNameAr { get; set; }
        public int LevelSN { get; set; }
        public string? LevelName { get; set; }
        public string? LevelNameAr { get; set; }
        public double Debit { get; set; }
        public IList<byte> Semesters { get; set; } = new List<byte>();
    }
}
