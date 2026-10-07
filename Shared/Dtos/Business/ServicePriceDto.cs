namespace Shared.Dtos.Business
{
    public class ServicePriceDto
    {
        public Guid Id { get; set; }
        public int ServiceId { get; set; }
        public int ClassId { get; set; }
        public string? ClassName { get; set; }
        public string? ClassNameAr { get; set; }
        public byte SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public string? SemesterNameAr { get; set; }
        public double Price { get; set; }

        public IList<byte> Semesters { get; set; } = new List<byte>();
        public IList<int> Classes { get; set; } = new List<int>();

        public bool IsEditable { get; set; }
        public bool IsDeletable { get; set; }
    }
}
