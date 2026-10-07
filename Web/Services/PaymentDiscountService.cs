namespace Web.Services;

public static class PaymentDiscountService
{
    // The ledger includes the proposed payment/refund, and excludes the old voucher.
    public static async Task<List<StudentAccount>> CalculateAsync(IUnitOfWork uow, Guid schoolId,
        StudentContract contract, Guid parentId, DateTime date, List<StudentAccount> accounts)
    {
        var rules = (await uow.DiscountDetails.FindAllAsync(x => x.SchoolId == schoolId
            && x.YearId == contract.RegisteredForYearId
            && x.Discount.ApplicationTiming == Shared.Enums.DiscountApplicationTiming.OnPayment, ["Discount"]))
            .OrderBy(x => x.Discount.SN).ThenBy(x => x.DiscountId).ToList();
        var semesters = (await uow.SchoolSemesters.FindAllAsync(x => x.SchoolId == schoolId
            && x.YearId == contract.RegisteredForYearId)).ToList();
        var siblingOrder = await uow.Students.CountAsync(x => x.Id != contract.StudentId && x.ParentId == parentId
            && x.SchoolId == schoolId && x.CurrentContract.RegisteredForYearId == contract.RegisteredForYearId
            && x.CurrentContract.StatusId >= 10 && x.CurrentContract.StatusId < 30) + 1;

        List<StudentAccount> Calculate(bool includeFullPayment)
        {
            var result = new List<StudentAccount>();
            foreach (var service in accounts.Where(x => x.Categoryid != StudentSubscriptionTax.TaxCategoryId)
                .GroupBy(x => new { x.ServiceId, x.SemesterId, x.Categoryid }))
            {
                var net = StudentAccountDiscountCalculator.CalculateNetServiceFee(accounts, contract.Id, service.Key.ServiceId, service.Key.SemesterId);
                foreach (var rule in rules.Where(x => x.Discount.ServiceCategoryId == service.Key.Categoryid).GroupBy(x => x.DiscountId))
                {
                    var reference = StudentDiscountWithdrawal.DiscountReference(rule.Key);
                    if (service.Any(x => x.RefNo == reference && x.Credit > 0)) continue;
                    var detail = rule.Where(x => x.SemesterId == service.Key.SemesterId || x.SemesterId == 0)
                        .OrderByDescending(x => x.SemesterId == service.Key.SemesterId)
                        .FirstOrDefault(x => StudentService.IsRegistrationStateInRange(x,
                            semesters.FirstOrDefault(s => s.SemesterId == service.Key.SemesterId), date, siblingOrder, contract.ClassId));
                    if (detail is null || !TransactionTypeIds.DiscountIds.Contains(detail.Discount.TransactionTypeId)) continue;
                    if (!includeFullPayment && detail.Discount.TransactionTypeId == TransactionTypeIds.FullPaymentDiscount) continue;
                    // Historical entries without a rule reference must not be awarded again.
                    if (service.Any(x => x.TransactionTypeId == detail.Discount.TransactionTypeId && x.Credit > 0
                        && (string.IsNullOrEmpty(x.RefNo) || !x.RefNo.StartsWith("discount:", StringComparison.Ordinal)))) continue;
                    var amount = StudentAccountDiscountCalculator.CalculateDiscountAmount(net, (decimal)detail.Amount, (decimal)detail.AdditionalFixedAmount);
                    if (amount <= 0) continue;
                    var entry = new StudentAccount
                    {
                        ContractId = contract.Id, ServiceId = service.Key.ServiceId, Categoryid = service.Key.Categoryid,
                        SemesterId = service.Key.SemesterId, TransactionTypeId = detail.Discount.TransactionTypeId,
                        Credit = (double)amount, Percentage = (decimal)detail.Amount, RefNo = reference, Comments = detail.Discount.NameAr
                    };
                    var tax = StudentSubscriptionTax.CreateDiscountEntry(entry, accounts.Concat(result));
                    result.Add(entry);
                    if (tax is not null) result.Add(tax);
                    net -= amount;
                }
            }
            return result;
        }

        var proposed = Calculate(true);
        return FullPaymentDiscountCalculator.IsTuitionSettled(accounts, [], proposed) ? proposed : Calculate(false);
    }
}
