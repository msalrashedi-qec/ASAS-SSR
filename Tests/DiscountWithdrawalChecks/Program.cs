using Core.Entities.Business;
using Core.Entities.Discounts;
using Core.Enums;
using Core.Services;

var contract = Guid.NewGuid();
var date = new DateTime(2026, 9, 17);
Discount Definition(int id, int sn, int type, bool cancel = true) => new()
{
    Id = id, SN = sn, TransactionTypeId = type, ServiceCategoryId = 20,
    IsCancelledOnWithdrawal = cancel, NameAr = $"خصم {id}"
};
StudentAccount Entry(int type, double credit, byte semester = 1, int service = 20, string? reference = null) => new()
{
    Id = Guid.NewGuid(), ContractId = contract, ServiceId = service, SemesterId = semester,
    Categoryid = 20, TransactionTypeId = type, Credit = credit, RefNo = reference
};
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine($"PASS: {name}");
}
var definitions = new[]
{
    Definition(1, 30, 200), Definition(2, 10, 400), Definition(3, 20, 500, false),
    Definition(4, 40, 700)
};
var ledger = new List<StudentAccount>
{
    Entry(200, 100), Entry(400, 50), Entry(200, 80, 2), Entry(200, 30, 1, 30),
    Entry(500, 90), Entry(100, 0)
};
var adjustment = Entry(200, 0);
adjustment.Debit = 10;
ledger.Add(adjustment);
IReadOnlyList<StudentAccount> Run(IEnumerable<StudentAccount> rows, IEnumerable<Discount>? rules = null) =>
    StudentDiscountWithdrawal.CreateEntries(rows, rules ?? definitions, contract, date, "test", date);
var result = Run(ledger);
Check(result.Count == 4 && result.Sum(x => x.Debit) == 250, "All awarded cancellable discounts across services and semesters; net of debit adjustments");
Check(result[0].RefNo == "discount-withdrawal:2", "Ascending SN order independent of definition and ledger order");
Check(result.All(x => x.TransactionTypeId == TransactionTypeIds.DiscountWithdrawal && x.Credit == 0
    && x.ContractId == contract && x.Date == date), "Withdrawal accounting and contract scope");
Check(Run(ledger.Concat(result)).Count == 0, "Repeated withdrawal creates no duplicate reversals");
result[0].Debit = 20;
var partial = Run(ledger.Concat(result));
Check(partial.Count == 1 && partial[0].Debit == 30, "Only the remaining amount after a partial reversal");
var otherContract = Entry(200, 900);
otherContract.ContractId = Guid.NewGuid();
Check(Run([otherContract]).Count == 0, "Other contracts excluded");
var sharedType = new[] { Definition(1, 20, 200), Definition(2, 10, 200, false) };
Check(Run([Entry(200, 70, reference: "discount:1"), Entry(200, 80, reference: "discount:2")], sharedType)
    .Single().Debit == 70, "Explicit references distinguish discounts sharing the transaction type");
var ambiguousRejected = false;
try { Run([Entry(200, 70)], sharedType); }
catch (InvalidOperationException) { ambiguousRejected = true; }
Check(ambiguousRejected, "Ambiguous legacy entries cannot cancel the wrong discount");

SiblingPolicyChecks.Run();
