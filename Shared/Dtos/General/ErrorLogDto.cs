namespace Shared.Dtos.General
{
    public class ErrorLogDto
    {
        public string UserName { get; set; } = string.Empty;
        public string UserNameAr { get; set; } = string.Empty;
        public string PageName { get; set; } = string.Empty;
        public string PageNameAr { get; set; } = string.Empty;
        public string IpAddress { get; set; }
        public string ErrorDetails { get; set; }
        public DateTime LoggedOn { get; set; }
    }
}
