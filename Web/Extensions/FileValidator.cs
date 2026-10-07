namespace Web.Extensions
{
    public static class FileValidator
    {
        public static bool ValidateSize(long fileSize, int maxSize)
        {
            return (fileSize / 1024 / 1024) <= maxSize;
        }

        public static bool ValidateType(string fileName)
        {
            string[] allowedExtenstions = new[] { ".jpg", ".gif", ".jfif", ".jpeg", ".png"
                , ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".rar", ".zip"
                , ".mp4", ".mov",".wmv",".avi",".flv", ".f4v",".swf",".mkv",".mpeg-2",".mp3",".wav",".aac",".flac",".m4a",".wma",".aiff"};
            return allowedExtenstions.Contains(Path.GetExtension(fileName).ToLower());
        }
        public static bool ValidateImage(string fileName)
        {
            string[] allowedExtenstions = new[] { ".jpg", ".gif", ".jfif", ".jpeg", ".png" };
            return allowedExtenstions.Contains(Path.GetExtension(fileName).ToLower());
        }
        public static bool ValidatePdf(string fileName)
        {
            return Path.GetExtension(fileName).ToLower() == ".pdf";
        }
        public static bool ValidateImageOrPdf(string fileName)
        {
            string[] allowedExtenstions = new[] { ".jpg", ".gif", ".jfif", ".jpeg", ".png", ".pdf" };
            return allowedExtenstions.Contains(Path.GetExtension(fileName).ToLower());
        }
    }
}
