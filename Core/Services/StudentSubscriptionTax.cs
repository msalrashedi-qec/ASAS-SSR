using Core.Entities.Business;
using Core.Enums;
using Core.Interfaces;

namespace Core.Services;

public static class StudentSubscriptionTax
{
    public const int TaxCategoryId = 90;

    public static async Task<StudentAccount?> CreateEntryAsync(IUnitOfWork uow, Guid schoolId,
        int nationalityId, StudentAccount fee)
    {
        // Tax settings are maintained per school, service and nationality, without a year.
        var setting = await uow.ServiceVATs.FindAsync(x => x.SchoolId == schoolId && x.ServiceId == fee.ServiceId && x.NationalityId == nationalityId);
        if (setting is null || setting.VAT <= 0)
            return null;

        var netFee = Math.Max(0, (decimal)fee.Debit - (decimal)fee.Credit);
        var amount = decimal.Round(netFee * setting.VAT, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0)
            return null;

        return new StudentAccount
        {
            ContractId = fee.ContractId,
            ServiceId = fee.ServiceId,
            SemesterId = fee.SemesterId,
            Categoryid = TaxCategoryId,
            TransactionTypeId = TransactionTypeIds.AddDues,
            Debit = (double)amount,
            Credit = 0,
            Percentage = setting.VAT,
            Date = fee.Date,
            Comments = $"إضافة ضريبة {(setting.VAT * 100).ToString("N0")} %",
            CreatedBy = fee.CreatedBy,
            CreatedOn = fee.CreatedOn
        };
    }

    // Pass the ledger BEFORE applying the service discount. Payments do not
    // change either basis. Fixed amounts use their effective share of the fee.
    public static StudentAccount? CreateDiscountEntry(StudentAccount discount, IEnumerable<StudentAccount> accounts)
    {
        if (discount.Categoryid == TaxCategoryId || discount.Credit <= 0
            || !TransactionTypeIds.DiscountIds.Contains(discount.TransactionTypeId))
            return null;
        var ledger = accounts.ToList();
        var fee = StudentAccountDiscountCalculator.CalculateNetServiceFee(ledger, discount.ContractId, discount.ServiceId, discount.SemesterId);
        if (fee <= 0) return null;
        var tax = ledger.Where(x => x.ContractId == discount.ContractId
            && x.ServiceId == discount.ServiceId && x.SemesterId == discount.SemesterId && x.Categoryid == TaxCategoryId
            && (x.TransactionTypeId == TransactionTypeIds.AddDues
                || x.TransactionTypeId == TransactionTypeIds.FinancialSettlement
                || TransactionTypeIds.DiscountIds.Contains(x.TransactionTypeId)))
            .Sum(x => (decimal)x.Debit - (decimal)x.Credit);
        var ratio = Math.Min(1m, (decimal)discount.Credit / fee);
        var amount = decimal.Round(Math.Max(0, tax) * ratio, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0) return null;
        return new StudentAccount
        {
            ContractId = discount.ContractId, ServiceId = discount.ServiceId,
            SemesterId = discount.SemesterId, Categoryid = TaxCategoryId,
            TransactionTypeId = discount.TransactionTypeId, RefNo = discount.RefNo,
            Credit = (double)amount, Percentage = ratio, Date = discount.Date,
            Comments = discount.Comments, CreatedBy = discount.CreatedBy, CreatedOn = discount.CreatedOn
        };
    }
}
