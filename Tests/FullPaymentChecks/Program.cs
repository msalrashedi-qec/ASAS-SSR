using Core.Entities.Business;
using Core.Enums;
using Core.Services;

var contract = Guid.NewGuid();
StudentAccount Entry(int type, double debit, double credit, byte semester = 1, int category = 20) => new()
{
    ContractId = contract, ServiceId = category, Categoryid = category, SemesterId = semester,
    TransactionTypeId = type, Debit = debit, Credit = credit
};
ReceiptDetail Payment(double amount, byte semester = 1, int category = 20) => new()
{
    ServiceId = category, CategoryId = category, SemesterId = semester, Amount = amount
};
void Check(bool value, string name)
{
    if (!value) throw new Exception(name);
    Console.WriteLine($"PASS: {name}");
}
var ledger = new List<StudentAccount>
{
    Entry(TransactionTypeIds.AddDues, 1000, 0),
    Entry(TransactionTypeIds.SiblingDiscount, 0, 100),
    Entry(TransactionTypeIds.DuesPayment, 0, 300)
};
var basis = StudentAccountDiscountCalculator.CalculateNetServiceFee(ledger, contract, 20, 1);
var discount = StudentAccountDiscountCalculator.CalculateDiscountAmount(basis, 10, 0);
Check(basis == 900 && discount == 90, "Discount basis includes previous discounts and excludes payments");
var proposed = new[] { Entry(TransactionTypeIds.FullPaymentDiscount, 0, (double)discount) };
Check(FullPaymentDiscountCalculator.IsTuitionSettled(ledger, [Payment(510)], proposed), "Previous payment plus new payment settles net tuition");
Check(!FullPaymentDiscountCalculator.IsTuitionSettled(ledger, [Payment(500)], proposed), "Partial payment does not award discount");
Check(!FullPaymentDiscountCalculator.IsTuitionSettled(ledger, [Payment(510, category: 30)], proposed), "Other service payment cannot settle tuition");
Check(!FullPaymentDiscountCalculator.IsTuitionSettled(ledger, [Payment(600)], proposed), "Nonzero credit balance does not meet exact-zero condition");
ledger.Add(Entry(TransactionTypeIds.AddDues, 1000, 0, 2));
Check(!FullPaymentDiscountCalculator.IsTuitionSettled(ledger, [Payment(510)], proposed), "Unpaid second semester prevents full-payment discount");
Check(FullPaymentDiscountCalculator.IsTuitionSettled(ledger, [Payment(510), Payment(1000, 2)], proposed), "All semesters settled");
ledger.Add(Entry(TransactionTypeIds.AddDues, 700, 0, category: 30));
Check(FullPaymentDiscountCalculator.IsTuitionSettled(ledger, [Payment(510), Payment(1000, 2)], proposed), "Non-tuition dues do not affect tuition eligibility");
Check(!FullPaymentDiscountCalculator.IsTuitionSettled([], [], []), "No tuition does not qualify");
Check(StudentAccountDiscountCalculator.CalculateDiscountAmount(100, 10, 15) == 25, "Configured fixed amount is added");
Check(StudentAccountDiscountCalculator.CalculateDiscountAmount(100, 150, 15) == 100, "Discount is capped at net tuition");

Check(ReceiptAllocation.IsValid([0, 40.10, 59.90], 100m), "Redistribution permits removing a line and preserves exact total");
Check(!ReceiptAllocation.IsValid([99.99], 100m), "One cent short is rejected");
Check(!ReceiptAllocation.IsValid([100.01], 100m), "One cent excess is rejected");
Check(!ReceiptAllocation.IsValid([-1, 101], 100m), "Negative allocations are rejected");
Check(!ReceiptAllocation.IsValid([0.001, 99.999], 100m), "Sub-cent allocations are rejected before saving");
Check(!ReceiptAllocation.IsValid([double.NaN], 100m), "Invalid numeric input is rejected");
Check(!ReceiptAllocation.IsValid([double.PositiveInfinity], 100m), "Infinite input is rejected");
Check(ReceiptAllocation.IsValid([0.1, 0.2], 0.3m), "Money comparison avoids binary floating point sum errors");
Check(ReceiptAllocation.MatchesLedger([(1, 20, 20, 100d)], [(1, 20, 20, 40d), (1, 20, 20, 60d)]), "Duplicate ledger lines are matched by aggregate");
Check(!ReceiptAllocation.MatchesLedger([(1, 20, 20, 100d)], [(1, 30, 20, 100d)]), "Incorrect ledger service is rejected");
var editedLedger = new[] { Entry(TransactionTypeIds.AddDues, 1000, 0), Entry(TransactionTypeIds.DuesPayment, 0, 900) };
Check(FullPaymentDiscountCalculator.IsTuitionSettled(editedLedger, [], [Entry(TransactionTypeIds.FullPaymentDiscount, 0, 100)]), "Edited payment is counted exactly once for discount eligibility");
Check(!FullPaymentDiscountCalculator.IsTuitionSettled(editedLedger.Append(Entry(TransactionTypeIds.FeeRefund, 100, 0)), [], [Entry(TransactionTypeIds.FullPaymentDiscount, 0, 100)]), "Refund debit participates in discount eligibility");
