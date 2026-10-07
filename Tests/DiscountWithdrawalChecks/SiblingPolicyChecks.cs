using Core.Entities.Business;
using Core.Entities.Discounts;
using Core.Enums;
using Core.Services;

internal static class SiblingPolicyChecks
{
    public static void Run()
    {
        Student Child(int rank) => new()
        {
            Id = Guid.NewGuid(),
            CurrentContract = new StudentContract
            {
                Id = Guid.NewGuid(), StatusId = 10,
                Class = new SchoolClass { SN = rank, Level = new SchoolLevel { SN = 1 } }
            }
        };
        var older = Child(2);
        var younger = Child(1);
        var definition = new Discount
        {
            Id = 1, ServiceCategoryId = 20, TransactionTypeId = TransactionTypeIds.SiblingDiscount
        };
        var ledger = new List<StudentAccount>();
        foreach (var child in new[] { older, younger })
        {
            foreach (var category in new[] { 20, 90 })
                ledger.Add(new StudentAccount
                {
                    ContractId = child.CurrentContract.Id, ServiceId = 20, SemesterId = 1,
                    Categoryid = category, TransactionTypeId = TransactionTypeIds.AddDues,
                    Debit = category == 20 ? 1000 : 150
                });
        }
        IReadOnlyList<StudentAccount> Reconcile() => SiblingDiscountPolicy.Reconcile(
            [older, younger], ledger, [definition], (_, _, _, _) =>
            [new DiscountDetail { DiscountId = 1, Discount = definition, AdditionalFixedAmount = 200 }],
            DateTime.Today, "test", DateTime.UtcNow);
        var changes = Reconcile();
        if (changes.Count != 2 || changes.Any(x => x.ContractId != younger.CurrentContract.Id)
            || changes.Single(x => x.Categoryid == 20).Credit != 200
            || changes.Single(x => x.Categoryid == 90).Credit != 30)
            throw new Exception("Sibling fixed discount must include matching tax discount only for eligible child.");
        ledger.AddRange(changes);
        if (Reconcile().Count != 0) throw new Exception("Sibling reconciliation must be idempotent.");
        younger.CurrentContract.Class.SN = 3;
        changes = Reconcile();
        if (changes.Where(x => x.ContractId == younger.CurrentContract.Id).Sum(x => x.Debit) != 230
            || changes.Where(x => x.ContractId == older.CurrentContract.Id).Sum(x => x.Credit) != 230)
            throw new Exception("Sibling rank changes must reverse and award both fee and tax discounts.");
        Console.WriteLine("PASS: Sibling tax discounts, idempotency and eligibility changes");
    }
}
