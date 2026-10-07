namespace Shared.Dtos.Business
{
    public class StudentDocumentDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string? StudentName { get; set; }
        public int YearId { get; set; }
        public string? YearName { get; set; }
        public int TypeId { get; set; }
        public string? TypeName { get; set; }
        public string? TypeNameAr { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public byte StatusId { get; set; }
        public string? StatusName { get; set; }
        public string? StatusNameAr { get; set; }
        public string? ColorCode { get; set; }
        public string? Comments { get; set; }
    }
}
