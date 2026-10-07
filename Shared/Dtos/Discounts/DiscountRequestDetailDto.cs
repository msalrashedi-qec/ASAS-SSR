
namespace Shared.Dtos.Discounts
{
    public class DiscountRequestDetailDto
    {
        public decimal RemainingAmount { get; set; }
        public bool IsSelected { get; set; }
        public Guid? ContractId { get; set; }
        public int? ServiceId { get; set; }
        public int? CategoryId { get; set; }
        public string? ItemName { get; set; }
        public string? ItemNameAr { get; set; }
        public byte SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public string? SemesterNameAr { get; set; }
        public double Percentage { get; set; }
        public double Amount { get; set; }
        public bool HasConfiguredPercentage { get; set; }
    }
}
