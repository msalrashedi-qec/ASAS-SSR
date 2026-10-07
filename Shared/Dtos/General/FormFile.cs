namespace Shared.Dtos.General
{
    public class FormFile
    {
        public FormFile(Stream fileStream, string fileName)
        {
            FileStream = fileStream;
            FileName = fileName;
        }
        public Stream FileStream { get; set; }
        public string FileName { get; set; }
    }
}
