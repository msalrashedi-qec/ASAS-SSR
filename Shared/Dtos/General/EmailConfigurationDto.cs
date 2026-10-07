namespace Shared.Dtos.General
{
    public class EmailConfigurationDto
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public bool IsBodyHTML { get; set; }
        public bool EnableSSL { get; set; }
        public string? ToEmail { get; set; }
        public string? ToName { get; set; }
        public string? CcEmail { get; set; }
        public byte TypeId { get; set; }
        public string? TypeName { get; set; }
        public string? TypeNameAr { get; set; }
        public string? CreatorName { get; set; }
        public string? CreatorNameAr { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}
