namespace Shared.Dtos.General
{
    public class ReportDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }
        public byte ModuleId { get; set; }
        public string? ModuleName { get; set; }
        public string? ModuleNameAr { get; set; }
        public string? Icon { get; set; }
        public string? Url { get; set; }
        public int SN { get; set; }
        public bool IsPermitted { get; set; } = false;
    }
}
