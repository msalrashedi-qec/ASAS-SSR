namespace Shared.Dtos.General
{
    public class CompanyDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }
        public string Logo { get; set; }
        public string CR { get; set; }
        public string Address { get; set; }
        public string? PO { get; set; }
        public string? PoCode { get; set; }
        public string PhoneNumber { get; set; }
        public string? FaxNumber { get; set; }
        public string? Email { get; set; }
        public string ReceiptFooter { get; set; }
    }
}
