using Core.Entities.Business;
using Core.Enums;

namespace Core.Services;

public static class FullPaymentDiscountCalculator
{
    public static List<StudentAccount> GetDiscountsToRemove(IEnumerable<StudentAccount> accounts, Guid contractId)
    {
        var ledger = accounts.Where(x => x.ContractId == contractId).ToList();
        var hasRemainingTuition = ledger.Where(x => x.Categoryid == 20)
            .GroupBy(x => new { x.ServiceId, x.SemesterId })
            .Any(group => decimal.Round(group.Sum(x => (decimal)x.Debit - (decimal)x.Credit),
                2, MidpointRounding.AwayFromZero) > 0);

        // Include the discount's VAT entries, including historical entries without a rule reference.
        return hasRemainingTuition
            ? ledger.Where(x => x.TransactionTypeId == TransactionTypeIds.FullPaymentDiscount).ToList()
            : [];
    }

    public static bool IsTuitionSettled(IEnumerable<StudentAccount> accounts,
        IEnumerable<ReceiptDetail> payments, IEnumerable<StudentAccount> discounts)
    {
        var tuition = accounts.Where(x => x.Categoryid == 20).ToList();
        return tuition.Count > 0 && tuition.GroupBy(x => new { x.ServiceId, x.SemesterId })
            .All(group => decimal.Round(group.Sum(x => (decimal)x.Debit - (decimal)x.Credit)
                - payments.Where(x => x.CategoryId == 20 && x.ServiceId == group.Key.ServiceId && x.SemesterId == group.Key.SemesterId).Sum(x => (decimal)x.Amount)
                - discounts.Where(x => x.Categoryid == 20 && x.ServiceId == group.Key.ServiceId && x.SemesterId == group.Key.SemesterId).Sum(x => (decimal)x.Credit),
                2, MidpointRounding.AwayFromZero) == 0);
    }
}
