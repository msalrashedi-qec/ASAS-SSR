using Core.Entities.Business;
using Core.Enums;

namespace Core.Services;

public sealed record SchoolFinanceSummary(decimal Previous, decimal Dues, decimal PreviousDiscount,
    decimal PreviousPaid, decimal Discount, decimal Paid)
{
    public decimal TotalPaid => PreviousPaid + Paid;
    public decimal Remaining => Previous + Dues - PreviousDiscount - Discount - TotalPaid;
}

public static class SchoolDashboardStatistics
{
    public static bool IsPayment(StudentAccount row) => row.TransactionTypeId is TransactionTypeIds.DuesPayment or TransactionTypeIds.FeeRefund;
    private static bool IsDiscount(StudentAccount row) => TransactionTypeIds.DiscountIds.Contains(row.TransactionTypeId)
        || row.TransactionTypeId is TransactionTypeIds.FinancialSettlement or TransactionTypeIds.FullPaymentSettlement;
    public static decimal Net(StudentAccount row) => (decimal)row.Debit - (decimal)row.Credit;

    public static SchoolFinanceSummary Finance(IEnumerable<StudentAccount> source)
    {
        var rows = source.Where(x => !x.IsDeleted).ToList();
        decimal Sum(bool previous, Func<StudentAccount, bool> predicate) => rows
            .Where(x => (x.Categoryid == 10) == previous && predicate(x)).Sum(Net);
        // Every ledger entry belongs to exactly one bucket. Withdrawals and tax
        // adjustments remain in dues, so the remaining balance reconciles to the ledger.
        return new(Sum(true, x => !IsPayment(x) && !IsDiscount(x)),
            Sum(false, x => !IsPayment(x) && !IsDiscount(x)),
            -Sum(true, IsDiscount), -Sum(true, IsPayment),
            -Sum(false, IsDiscount), -Sum(false, IsPayment));
    }

    public static int ActiveAt(IEnumerable<StudentContract> contracts, DateTime date) => contracts
        .Where(x => !x.IsDeleted && x.StartDate.Date <= date.Date && x.EndDate.Date >= date.Date
            && (!x.ActualEndDate.HasValue || x.ActualEndDate.Value.Date > date.Date)
            && (x.StatusId is 10 or 20 || x.ActualEndDate.HasValue && x.RegisteredForYearId <= x.YearId))
        .Select(x => x.StudentId).Distinct().Count();
}
