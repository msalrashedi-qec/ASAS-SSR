namespace Shared.Dtos.Business
{
    public class SchoolDto
    {
        public Guid Id { get; set; }
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyNameAr { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }
        public byte TypeId { get; set; }
        public string? TypeName { get; set; }
        public string? TypeNameAr { get; set; }
        public int CityId { get; set; }
        public string? CityName { get; set; }
        public string? CityNameAr { get; set; }
        public string Logo { get; set; }
        public string Address { get; set; }
        public string? PO { get; set; }
        public string? PoCode { get; set; }
        public string Email { get; set; }
        public string SmsSender { get; set; } = string.Empty;
        public string SmsUserName { get; set; } = string.Empty;
        public string SmsPassword { get; set; } = string.Empty;

        public string ChequePayeeName { get; set; }
        public string BankName { get; set; }
        public string IBAN { get; set; }
    }
}
