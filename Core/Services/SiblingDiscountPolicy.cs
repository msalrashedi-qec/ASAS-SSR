using Core.Entities.Business;
using Core.Entities.Discounts;
using Core.Enums;

namespace Core.Services;

public static class SiblingDiscountPolicy
{
    public static IReadOnlyList<Student> Rank(IEnumerable<Student> students) => students
        .Where(x => x.CurrentContract.StatusId >= 10 && x.CurrentContract.StatusId < 30)
        .OrderByDescending(x => x.CurrentContract.Class.Level.SN)
        .ThenByDescending(x => x.CurrentContract.Class.SN)
        .ThenByDescending(x => x.CurrentContract.CreatedOn)
        .ThenByDescending(x => x.CreatedOn)
        .ThenByDescending(x => x.SN)
        .ThenBy(x => x.Id).ToList();

    // Call with one parent's students in one school and academic year.
    // Existing positive awards retain their agreed amount; missing awards are
    // calculated from the configured rules. The ledger makes this idempotent.
    public static IReadOnlyList<StudentAccount> Reconcile(
        IEnumerable<Student> family, IEnumerable<StudentAccount> accounts,
        IEnumerable<Discount> discounts,
        Func<Student, int, byte, int, IEnumerable<DiscountDetail>> applicableDetails,
        DateTime date, string userId, DateTime now)
    {
        var ranked = Rank(family);
        var ledger = accounts.ToList();
        var definitions = discounts.Where(x => x.TransactionTypeId == TransactionTypeIds.SiblingDiscount).ToList();
        var reversalReferences = definitions.Select(x => $"discount-withdrawal:{x.Id}").ToHashSet();
        var changes = new List<StudentAccount>();
        for (var index = 0; index < ranked.Count; index++)
        {
            var student = ranked[index];
            var contractId = student.CurrentContract.Id;
            if (index == 0)
            {
                changes.AddRange(StudentDiscountWithdrawal.CreateEntries(ledger, definitions,
                    contractId, date, userId, now, enforceEligibility: true));
                continue;
            }

            var fees = ledger.Where(x => x.ContractId == contractId
                    && x.TransactionTypeId == TransactionTypeIds.AddDues && x.Categoryid != 10 && x.Categoryid != StudentSubscriptionTax.TaxCategoryId)
                .GroupBy(x => (x.ServiceId, x.SemesterId, x.Categoryid)).ToList();
            foreach (var fee in fees)
            {
                var scope = ledger.Where(x => x.ContractId == contractId && x.ServiceId == fee.Key.ServiceId
                    && x.SemesterId == fee.Key.SemesterId && x.Categoryid == fee.Key.Categoryid).ToList();
                // Do not restore discounts for a service that has been withdrawn.
                var outstandingFees = scope.Where(x => x.TransactionTypeId == TransactionTypeIds.AddDues
                    || x.TransactionTypeId == TransactionTypeIds.DuesWithdrawal)
                    .Sum(x => (decimal)x.Debit - (decimal)x.Credit);
                if (outstandingFees <= 0)
                    continue;
                var awarded = scope.Where(x => x.TransactionTypeId == TransactionTypeIds.SiblingDiscount
                    || (x.TransactionTypeId == TransactionTypeIds.DiscountWithdrawal && reversalReferences.Contains(x.RefNo ?? "")))
                    .Sum(x => (decimal)x.Credit - (decimal)x.Debit);
                if (awarded > 0)
                    continue;

                var details = applicableDetails(student, index + 1, fee.Key.SemesterId, fee.Key.Categoryid)
                    .OrderBy(x => x.Discount.SN).ThenBy(x => x.DiscountId).ToList();
                // Categories with no sibling-discount configuration are outside this policy.
                if (!definitions.Any(x => x.ServiceCategoryId == fee.Key.Categoryid))
                    continue;
                if (details.Count == 0)
                    throw new InvalidOperationException($"لا يوجد إعداد خصم أخوة مطابق للطالب {student.Name} للفصل {fee.Key.SemesterId}.");
                foreach (var detail in details)
                {
                    var netFee = StudentAccountDiscountCalculator.CalculateNetServiceFee(ledger, contractId, fee.Key.ServiceId, fee.Key.SemesterId);
                    var amount = StudentAccountDiscountCalculator.CalculateDiscountAmount(netFee,
                        (decimal)detail.Amount, (decimal)detail.AdditionalFixedAmount);
                    if (amount <= 0)
                        continue;
                    var entry = new StudentAccount
                    {
                        ContractId = contractId, ServiceId = fee.Key.ServiceId, SemesterId = fee.Key.SemesterId,
                        Categoryid = fee.Key.Categoryid, TransactionTypeId = TransactionTypeIds.SiblingDiscount,
                        RefNo = StudentDiscountWithdrawal.DiscountReference(detail.DiscountId),
                        Credit = (double)amount, Percentage = (decimal)detail.Amount / 100m,
                        Date = date, CreatedBy = userId, CreatedOn = now,
                        Comments = detail.Comments ?? detail.Discount.Name
                    };
                    var taxDiscount = StudentSubscriptionTax.CreateDiscountEntry(entry, ledger);
                    if (taxDiscount is not null)
                    {
                        changes.Add(taxDiscount);
                        ledger.Add(taxDiscount);
                    }
                    changes.Add(entry);
                    ledger.Add(entry);
                }
            }
        }
        return changes;
    }
}
