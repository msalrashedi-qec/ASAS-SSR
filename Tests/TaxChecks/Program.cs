using System.Linq.Expressions;
using System.Reflection;
using Core.Entities.Business;
using Core.Entities.Discounts;
using Core.Enums;
using Core.Interfaces;
using Core.Services;

void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine($"PASS: {name}"); }
var school = Guid.NewGuid();
var setting = new ServiceVAT { SchoolId = school, ServiceId = 20, NationalityId = 2, VAT = 15 };
var repository = DispatchProxy.Create<IBaseRepository<ServiceVAT>, Stub>();
((Stub)(object)repository).Handler = (_, args) => Task.FromResult(
    ((Expression<Func<ServiceVAT, bool>>)args![0]!).Compile()(setting) ? setting : null!);
var uow = DispatchProxy.Create<IUnitOfWork, Stub>();
((Stub)(object)uow).Handler = (method, _) => method.Name == "get_ServiceVATs" ? repository : throw new Exception(method.Name);
var fee = new StudentAccount { ContractId = Guid.NewGuid(), ServiceId = 20, SemesterId = 1,
    Categoryid = 20, TransactionTypeId = TransactionTypeIds.AddDues, Debit = 1000 };
var tax = await StudentSubscriptionTax.CreateEntryAsync(uow, school, 2, fee);
Check(tax?.Debit == 150 && tax.Categoryid == 90, "Tax on gross subscription fee");
Check(await StudentSubscriptionTax.CreateEntryAsync(uow, Guid.NewGuid(), 2, fee) is null, "School isolation");
Check(await StudentSubscriptionTax.CreateEntryAsync(uow, school, 3, fee) is null, "Nationality isolation");
setting.ServiceId = 30;
Check(await StudentSubscriptionTax.CreateEntryAsync(uow, school, 2, fee) is null, "Service isolation");
setting.ServiceId = 20; setting.VAT = 0;
Check(await StudentSubscriptionTax.CreateEntryAsync(uow, school, 2, fee) is null, "Zero tax");
var ledger = new List<StudentAccount> { fee, tax! };
StudentAccount Discount(double amount) => new() { ContractId = fee.ContractId, ServiceId = 20, SemesterId = 1,
    Categoryid = 20, TransactionTypeId = TransactionTypeIds.SiblingDiscount, Credit = amount,
    RefNo = StudentDiscountWithdrawal.DiscountReference(1) };
var fixedDiscount = Discount(200);
var fixedTax = StudentSubscriptionTax.CreateDiscountEntry(fixedDiscount, ledger);
Check(fixedTax?.Credit == 30 && fixedTax.Percentage == .2m, "Fixed 200 of 1000 discounts 30 of 150 tax");
ledger.Add(fixedDiscount); ledger.Add(fixedTax!);
var nextDiscount = Discount(80);
var nextTax = StudentSubscriptionTax.CreateDiscountEntry(nextDiscount, ledger);
Check(nextTax?.Credit == 12 && nextTax.Percentage == .1m, "Sequential ten percent discount");
ledger.Add(new StudentAccount { ContractId = fee.ContractId, ServiceId = 20, SemesterId = 1, Categoryid = 90,
    TransactionTypeId = TransactionTypeIds.DuesPayment, Credit = 100 });
Check(StudentSubscriptionTax.CreateDiscountEntry(nextDiscount, ledger)?.Credit == 12, "Tax payments do not reduce discount basis");
Check(StudentSubscriptionTax.CreateDiscountEntry(Discount(800), ledger)?.Credit == 120, "Full discount removes remaining tax");
Check(StudentSubscriptionTax.CreateDiscountEntry(Discount(1000), ledger)?.Credit == 120, "Discount tax capped at remaining tax");
Check(StudentSubscriptionTax.CreateDiscountEntry(Discount(100), [fee]) is null, "Exempt subscription has no tax discount");
var definitions = new[] { new Discount { Id = 1, ServiceCategoryId = 20, TransactionTypeId = TransactionTypeIds.SiblingDiscount, IsCancelledOnWithdrawal = true } };
var reversed = StudentDiscountWithdrawal.CreateEntries(ledger, definitions, fee.ContractId, DateTime.Today, "test", DateTime.UtcNow);
Check(reversed.Count == 2 && reversed.Single(x => x.Categoryid == 90).Debit == 30, "Withdrawal reverses service and tax discount");
ledger.AddRange(reversed);
Check(StudentDiscountWithdrawal.CreateEntries(ledger, definitions, fee.ContractId, DateTime.Today, "test", DateTime.UtcNow).Count == 0, "Withdrawal is idempotent");
public class Stub : DispatchProxy
{
    public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;
    protected override object? Invoke(MethodInfo? method, object?[]? args) => Handler(method!, args);
}
