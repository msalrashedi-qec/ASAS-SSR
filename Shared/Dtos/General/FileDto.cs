namespace Shared.Dtos.General
{
    public class FileDto
    {
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public double? FileSize { get; set; }
        public bool IsNew { get; set; }
    }
}
