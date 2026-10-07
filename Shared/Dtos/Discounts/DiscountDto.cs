namespace Shared.Dtos.Discounts
{
    public class DiscountDto
    {
        public int Id { get; set; }
        public int SN { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }
        public int TransactionTypeId { get; set; }
        public string? TransactionTypeName { get; set; }
        public string? TransactionTypeNameAr { get; set; }
        public byte TypeId { get; set; }
        public string? TypeName { get; set; }
        public string? TypeNameAr { get; set; }
        public int ServiceCategoryId { get; set; }
        public string? ServiceCategoryName { get; set; }
        public string? ServiceCategoryNameAr { get; set; }
        public byte DependencyId { get; set; }
        public string? DependencyName { get; set; }
        public string? DependencyNameAr { get; set; }
        public Shared.Enums.DiscountApplicationTiming ApplicationTiming { get; set; } = Shared.Enums.DiscountApplicationTiming.OnSubscription;
        public bool IsCancelledOnWithdrawal { get; set; }
        public bool IsShownInContract { get; set; }

        public List<DiscountDetailDto> Details { get; set; } = new();
    }
}
