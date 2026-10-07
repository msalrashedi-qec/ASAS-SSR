namespace Shared.Dtos.General
{
    public class ServiceTaxDto
    {
        public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        public int ServiceId { get; set; }
        public int NationalityId { get; set; }
        public decimal VAT { get; set; }

        public string? ServiceName { get; set; }
        public string? ServiceNameAr { get; set; }
        public string? NationalityName { get; set; }
        public string? NationalityNameAr { get; set; }

        public IList<int> Nationalities { get; set; } = new List<int>();
        
    }
}
