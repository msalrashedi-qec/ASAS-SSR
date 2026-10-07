using Core.Entities.Business;
using Core.Entities.Discounts;
using Core.Enums;

namespace Core.Services;

public static class StudentDiscountWithdrawal
{
    public static string DiscountReference(int discountId) => $"discount:{discountId}";

    public static IReadOnlyList<StudentAccount> CreateEntries( IEnumerable<StudentAccount> accounts, IEnumerable<Discount> discounts, Guid contractId, DateTime date, string userId, DateTime now, bool enforceEligibility = false)
    {
        var ledger = accounts.Where(x => x.ContractId == contractId).ToList();
        var definitions = discounts.ToList();
        var assigned = new List<(StudentAccount Account, Discount Discount)>();
        foreach (var account in ledger.Where(x => x.TransactionTypeId != TransactionTypeIds.DiscountWithdrawal))
        {
            var candidates = definitions.Where(x => x.TransactionTypeId == account.TransactionTypeId && (x.ServiceCategoryId == account.Categoryid || (account.Categoryid == StudentSubscriptionTax.TaxCategoryId && account.RefNo == DiscountReference(x.Id)))).ToList();
            if (!candidates.Any(x => enforceEligibility || x.IsCancelledOnWithdrawal))
                continue;

            var discount = candidates.SingleOrDefault(x => account.RefNo == DiscountReference(x.Id));
            if (discount is null)
            {
                // Older ledger entries did not record the discount identity. Only
                // infer it when the transaction type and category are unambiguous.
                if (candidates.Count != 1 || account.RefNo?.StartsWith("discount:", StringComparison.Ordinal) == true)
                    throw new InvalidOperationException($"تعذر تحديد الخصم للقيد {account.Id}. يجب مراجعة مرجع الخصم ونوع الحركة والتصنيف قبل الانسحاب.");
                discount = candidates[0];
            }
            if (enforceEligibility || discount.IsCancelledOnWithdrawal)
                assigned.Add((account, discount));
        }

        var result = new List<StudentAccount>();
        foreach (var group in assigned.GroupBy(x => new
                 { x.Discount.Id, x.Discount.SN, x.Account.ServiceId, x.Account.SemesterId, x.Account.Categoryid })
                 .OrderBy(x => x.Key.SN).ThenBy(x => x.Key.Id)
                 .ThenBy(x => x.Key.ServiceId).ThenBy(x => x.Key.SemesterId))
        {
            var reference = $"discount-withdrawal:{group.Key.Id}";
            var alreadyWithdrawn = ledger.Where(x => x.TransactionTypeId == TransactionTypeIds.DiscountWithdrawal
                && x.RefNo == reference && x.ServiceId == group.Key.ServiceId
                && x.SemesterId == group.Key.SemesterId && x.Categoryid == group.Key.Categoryid)
                .Sum(x => (decimal)x.Debit - (decimal)x.Credit);
            var amount = decimal.Round(group.Sum(x => (decimal)x.Account.Credit - (decimal)x.Account.Debit) - alreadyWithdrawn, 2, MidpointRounding.AwayFromZero);
            if (amount <= 0)
                continue;

            result.Add(new StudentAccount
            {
                ContractId = contractId,
                ServiceId = group.Key.ServiceId,
                SemesterId = group.Key.SemesterId,
                Categoryid = group.Key.Categoryid,
                TransactionTypeId = TransactionTypeIds.DiscountWithdrawal,
                RefNo = reference,
                Debit = (double)amount,
                Date = date,
                Comments = $"سحب {group.First().Discount.NameAr}",
                CreatedBy = userId,
                CreatedOn = now
            });
        }
        return result;
    }
}
