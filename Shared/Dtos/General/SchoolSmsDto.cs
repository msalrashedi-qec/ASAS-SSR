namespace Shared.Dtos.General
{
    public class SchoolSmsDto
    {
        public Guid SchoolId { get; set; }
        public string? Name { get; set; }
        public string? NameAr { get; set; }
        public string? SmsSender { get; set; }
        public string? SmsUserName { get; set; }
        public string? SmsPassword { get; set; }
    }
}
