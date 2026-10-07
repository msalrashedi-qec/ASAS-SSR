namespace Shared.Dtos.Discounts
{
    public class DiscountDetailDto
    {
        public Guid Id { get; set; }
        public int DiscountId { get; set; }
        public string? DiscountName { get; set; }
        public string? DiscountNameAr { get; set; }
        public Guid SchoolId { get; set; }
        public byte SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public string? SemesterNameAr { get; set; }
        public string FromRange { get; set; }
        public string ToRange { get; set; }
        public double Amount { get; set; }
        public double AdditionalFixedAmount { get; set; }
        public string? Comments { get; set; }

    }
}
