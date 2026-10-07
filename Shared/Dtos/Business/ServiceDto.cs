namespace Shared.Dtos.Business
{
    public class ServiceDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }
        public Guid SchoolId { get; set; }
        public string? SchoolName { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryNameAr { get; set; }
        public int YearId { get; set; }
        public string? YearName { get; set; }
        public decimal DefaultTax { get; set; }
        public List<ServicePriceDto> ServicePrices { get; set; } = new();
    }
}
