using Core.Entities.Business;
using Core.Entities.Discounts;

namespace Core.Entities.General
{
    public class TransactionType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameAr { get; set; }

        public virtual ICollection<StudentAccount> StudentAccounts { get; set; } = new List<StudentAccount>();
        public virtual ICollection<Discount> Discounts { get; set; } = new List<Discount>();

    }
}
