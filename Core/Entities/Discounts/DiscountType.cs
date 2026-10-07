namespace Core.Entities.Discounts
{
    public class DiscountType
    {
        public byte Id { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }

        public virtual ICollection<Discount> Discounts { get; set; } = new List<Discount>();

    }
}
