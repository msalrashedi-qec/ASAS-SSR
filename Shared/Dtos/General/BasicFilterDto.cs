namespace Shared.Dtos.General
{
    public class BasicFilterDto
    {
        public string Language { get; set; } = "ar";
        public int ReportTypeId { get; set; }
        public Guid? StudentId { get; set; }
        public Guid? ParentId { get; set; }
        public Guid? SchoolId { get; set; }
        public int? LevelId { get; set; }
        public int? FileNo { get; set; }
        public int? UserFileNo { get; set; }
        public string? RefNo { get; set; }
        public string? Name { get; set; }
        public string? NationalID { get; set; }
        public string? Email { get; set; }
        public List<int>? Ous { get; set; }
        public List<byte>? Statuses { get; set; } = null;
        public List<byte>? Genders { get; set; } = null;
        public List<int>? Nationalities { get; set; } = null;
        public List<int>? Types { get; set; } = null;
        public List<int>? Categories { get; set; } = null;
        public List<int>? Reasons { get; set; } = null;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool SelectAll { get; set; } = true;

        public byte SortColumn { get; set; } = 100;
        public string OrderBy { get; set; } = "DESC";
    }
}
