using System.Linq.Expressions;
using System.Reflection;
using Core.Entities.Business;
using Core.Entities.Discounts;
using Core.Enums;
using Core.Interfaces;
using Core.Services;
using Web.Services;

var school = Guid.NewGuid();
var parent = Guid.NewGuid();
var contract = new StudentContract { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), RegisteredForYearId = 2026 };
var rules = new List<DiscountDetail>();
var uow = Stub<IUnitOfWork>.Create((method, _) => method.Name switch
{
    "get_DiscountDetails" => Repository(rules),
    "get_SchoolSemesters" => Repository(new List<SchoolSemester>()),
    "get_Students" => Repository(new List<Student>()),
    _ => throw new NotSupportedException(method.Name)
});
IBaseRepository<T> Repository<T>(List<T> data) where T : class => Stub<IBaseRepository<T>>.Create((method, args) =>
{
    if (method.Name == "CountAsync") return Task.FromResult(0);
    if (method.Name == "FindAllAsync")
        return Task.FromResult<IEnumerable<T>>(data.Where(((Expression<Func<T, bool>>)args![0]!).Compile()).ToList());
    throw new NotSupportedException(method.Name);
});
DiscountDetail Rule(int id, int type, double percent) => new()
{
    SchoolId = school, YearId = 2026, SemesterId = 0, DiscountId = id, Amount = percent,
    Discount = new Discount { Id = id, SN = id, ServiceCategoryId = 20, TransactionTypeId = type,
        DependencyId = 0, ApplicationTiming = Shared.Enums.DiscountApplicationTiming.OnPayment }
};
StudentAccount Entry(int type, double debit, double credit, int category = 20, string? reference = null) => new()
{
    ContractId = contract.Id, ServiceId = 20, SemesterId = 1, Categoryid = category,
    TransactionTypeId = type, Debit = debit, Credit = credit, RefNo = reference
};
void Check(bool success, string name)
{
    if (!success) throw new Exception(name);
    Console.WriteLine($"PASS: {name}");
}
Task<List<StudentAccount>> Calculate(List<StudentAccount> ledger) =>
    PaymentDiscountService.CalculateAsync(uow, school, contract, parent, new DateTime(2026, 9, 27), ledger);

rules.Add(Rule(1, TransactionTypeIds.FullPaymentDiscount, 10));
var ledger = new List<StudentAccount> { Entry(TransactionTypeIds.AddDues, 1000, 0), Entry(TransactionTypeIds.DuesPayment, 0, 900) };
var discounts = await Calculate(ledger);
Check(discounts.Count == 1 && discounts[0].Credit == 100, "Edited payment qualifies for full-payment discount");
ledger.AddRange(discounts);
Check((await Calculate(ledger)).Count == 0, "Saving again does not duplicate an awarded discount");
ledger.RemoveRange(2, ledger.Count - 2);
ledger[1].Credit = 800;
Check((await Calculate(ledger)).Count == 0, "Partial settlement does not award full-payment discount");
ledger[1].Credit = 900;
ledger.Add(Entry(TransactionTypeIds.AddDues, 150, 0, 90));
discounts = await Calculate(ledger);
Check(discounts.Single(x => x.Categoryid == 90).Credit == 15, "Awarded discount includes the corresponding VAT reduction");
ledger.Add(Entry(TransactionTypeIds.FeeRefund, 100, 0));
Check((await Calculate(ledger)).Count == 0, "Refund prevents eligibility when tuition is no longer settled");
ledger.RemoveAt(ledger.Count - 1);
ledger.Add(Entry(TransactionTypeIds.FullPaymentDiscount, 0, 100, reference: "old-reference"));
Check((await Calculate(ledger)).Count == 0, "Historical discount references do not permit duplicate awards");

var awarded = Entry(TransactionTypeIds.FullPaymentDiscount, 0, 100, reference: "old-reference");
var awardedTax = Entry(TransactionTypeIds.FullPaymentDiscount, 0, 15, 90);
var siblingDiscount = Entry(TransactionTypeIds.SiblingDiscount, 0, 50);
var editedPayment = Entry(TransactionTypeIds.DuesPayment, 0, 850);
var edited = new List<StudentAccount>
{
    Entry(TransactionTypeIds.AddDues, 1000, 0), editedPayment, awarded, awardedTax, siblingDiscount,
    Entry(TransactionTypeIds.AddDues, 150, 0, 90)
};
Check(FullPaymentDiscountCalculator.GetDiscountsToRemove(edited, contract.Id).Count == 0,
    "Settled tuition retains its discount even with outstanding VAT");
editedPayment.Credit = 800;
var removed = FullPaymentDiscountCalculator.GetDiscountsToRemove(edited, contract.Id);
Check(removed.Count == 2 && removed.Contains(awarded) && removed.Contains(awardedTax) && !removed.Contains(siblingDiscount),
    "Redistribution leaving tuition due removes historical full-payment discount and VAT only");
Check(!(await Calculate(edited.Except(removed).ToList())).Any(x => x.TransactionTypeId == TransactionTypeIds.FullPaymentDiscount),
    "Removed full-payment discount is not awarded again for partial payment");
editedPayment.Credit = 900;
Check(FullPaymentDiscountCalculator.GetDiscountsToRemove(edited, contract.Id).Count == 0,
    "Tuition overpayment does not remove an existing discount");
editedPayment.Credit = 849.99;
Check(FullPaymentDiscountCalculator.GetDiscountsToRemove(edited, contract.Id).Count == 2,
    "One cent of remaining tuition removes the discount");
var otherSemesterCredit = Entry(TransactionTypeIds.DuesPayment, 0, 100);
otherSemesterCredit.SemesterId = 2;
edited.Add(otherSemesterCredit);
Check(FullPaymentDiscountCalculator.GetDiscountsToRemove(edited, contract.Id).Count == 2,
    "Credit in another semester cannot hide unpaid tuition");
Check(FullPaymentDiscountCalculator.GetDiscountsToRemove(edited, Guid.NewGuid()).Count == 0,
    "Discount removal is restricted to the edited contract");

rules.Clear();
rules.Add(Rule(2, TransactionTypeIds.EarlyPaymentDiscount, 5));
ledger = [Entry(TransactionTypeIds.AddDues, 1000, 0), Entry(TransactionTypeIds.DuesPayment, 0, 100)];
Check((await Calculate(ledger)).Single().Credit == 50, "Other discounts configured for payment timing are evaluated");
rules[0].Discount.DependencyId = 99;
Check((await Calculate(ledger)).Count == 0, "Unmet eligibility dependency prevents award");
rules[0].Discount.DependencyId = 0;
ledger.Add(Entry(TransactionTypeIds.EarlyPaymentDiscount, 0, 50, reference: StudentDiscountWithdrawal.DiscountReference(2)));
rules.Add(Rule(3, TransactionTypeIds.EarlyPaymentDiscount, 10));
discounts = await Calculate(ledger);
Check(discounts.Count == 1 && discounts[0].RefNo == StudentDiscountWithdrawal.DiscountReference(3)
    && discounts[0].Credit == 95, "An unawarded rule of the same type is calculated on the remaining fee");

List<object> Rows(object editor, object voucher, List<StudentAccount> entries)
{
    foreach (var entry in entries)
    {
        entry.Semester = new Core.Entities.General.Semester { Name = "Semester" };
        entry.Service = new Service { Name = "Service" };
        entry.Category = new Core.Entities.General.ServiceCategory { Name = "Category" };
    }
    return ((System.Collections.IEnumerable)editor.GetType().GetMethod("BuildDetails", BindingFlags.Instance | BindingFlags.NonPublic)!
        .Invoke(editor, [voucher, entries])!).Cast<object>().ToList();
}
object Value(object row, string property) => row.GetType().GetProperty(property)!.GetValue(row)!;
var receipt = new Receipt { SN = 123, ReceiptDetails = [new ReceiptDetail { SemesterId = 1, ServiceId = 20, CategoryId = 20, Amount = 100 }] };
var paymentEntries = new List<StudentAccount> { Entry(TransactionTypeIds.AddDues, 100, 0), Entry(TransactionTypeIds.DuesPayment, 0, 100, reference: "123") };
var unpaid = Entry(TransactionTypeIds.AddDues, 250, 0);
unpaid.ServiceId = 30;
paymentEntries.Add(unpaid);
var rows = Rows(new Web.Components.Pages.Business.ReceiptEdit(), receipt, paymentEntries);
Check(rows.Count == 2 && rows.Any(x => (int)Value(x, "ServiceId") == 30 && (double)Value(x, "Amount") == 0),
    "Receipt editor includes unpaid services absent from the original voucher");
Check((decimal)Value(rows.Single(x => (int)Value(x, "ServiceId") == 20), "Available") == 100,
    "Original payment is restored to available balance before redistribution");
var refund = new RefundReceipt { SN = 123, ReceiptDetails = [new RefundReceiptDetail { SemesterId = 1, ServiceId = 20, CategoryId = 20, Amount = 100 }] };
var refundEntries = new List<StudentAccount> { Entry(TransactionTypeIds.DuesPayment, 0, 100), Entry(TransactionTypeIds.FeeRefund, 100, 0, reference: "123") };
var credit = Entry(TransactionTypeIds.DuesPayment, 0, 250);
credit.ServiceId = 30;
refundEntries.Add(credit);
rows = Rows(new Web.Components.Pages.Business.RefundReceiptEdit(), refund, refundEntries);
Check(rows.Count == 2 && (decimal)Value(rows.Single(x => (int)Value(x, "ServiceId") == 20), "Available") == 100,
    "Refund editor restores the original refund and includes other credit balances");

public class Stub<T> : DispatchProxy where T : class
{
    private Func<MethodInfo, object?[]?, object?> handler = null!;
    public static T Create(Func<MethodInfo, object?[]?, object?> handler)
    {
        var value = Create<T, Stub<T>>();
        ((Stub<T>)(object)value).handler = handler;
        return value;
    }
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => handler(targetMethod!, args);
}
