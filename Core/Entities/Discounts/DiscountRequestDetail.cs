using Core.Entities.General;

namespace Core.Entities.Discounts;

public class DiscountRequestDetail
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid? ContractId { get; set; }
        public int? ServiceId { get; set; }
        public int? CategoryId { get; set; }
        public string? ItemName { get; set; }
        public string? ItemNameAr { get; set; }
        public byte SemesterId { get; set; }
    public double Percentage { get; set; }
    public double Amount { get; set; }

    public virtual DiscountRequest Request { get; set; } = null!;
    public virtual Semester Semester { get; set; } = null!;
}
