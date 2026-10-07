using Core.Entities.Business;
using Core.Enums;

namespace Core.Services;

/// <summary>
/// Calculates the amount on which the next discount must be applied.
/// The source of truth is the student's account ledger for one service and semester.
/// </summary>
public static class StudentAccountDiscountCalculator
{
    public const int ServiceFeeTransactionTypeId = TransactionTypeIds.AddDues;
    public const int SettlementTransactionTypeId = TransactionTypeIds.FinancialSettlement;

    private static readonly HashSet<int> DiscountTransactionTypeIds =
        [
            TransactionTypeIds.MarketingDiscount,
            TransactionTypeIds.PromotionalDiscount,
            TransactionTypeIds.LateRegistrationDiscount,
            TransactionTypeIds.SiblingDiscount,
            TransactionTypeIds.EmployeeChildrenDiscount,
            TransactionTypeIds.FullPaymentDiscount,
            TransactionTypeIds.EarlyPaymentDiscount,
            TransactionTypeIds.SpecialDiscount,
            TransactionTypeIds.ServiceDiscount,
            TransactionTypeIds.DiscountWithdrawal
        ];

    public static decimal CalculateNetServiceFee(IEnumerable<StudentAccount> accounts, Guid contractId, int serviceId, byte semesterId)
    {
        var matchingAccounts = accounts.Where(x => x.ContractId == contractId && x.ServiceId == serviceId && x.SemesterId == semesterId
            && x.Categoryid != StudentSubscriptionTax.TaxCategoryId);
        var serviceFees = matchingAccounts.Where(x => x.TransactionTypeId == ServiceFeeTransactionTypeId)
            .Sum(x => (decimal)x.Debit - (decimal)x.Credit);

        // Credit discounts/settlements reduce the basis. A debit settlement reverses
        // part of a previous reduction and therefore increases the basis again.
        var previousReductions = matchingAccounts
            .Where(x => x.TransactionTypeId == SettlementTransactionTypeId || DiscountTransactionTypeIds.Contains(x.TransactionTypeId))
            .Sum(x => (decimal)x.Credit - (decimal)x.Debit);

        return Math.Max( 0, decimal.Round(serviceFees - previousReductions, 2, MidpointRounding.AwayFromZero));
    }

    public static decimal CalculateDiscountAmount(decimal netServiceFee, decimal percentage, decimal additionalFixedAmount = 0)
    {
        if (netServiceFee <= 0)
            return 0;

        var calculated = decimal.Round(netServiceFee * percentage / 100m + additionalFixedAmount, 2, MidpointRounding.AwayFromZero);

        return Math.Min(netServiceFee, Math.Max(0, calculated));
    }
}
