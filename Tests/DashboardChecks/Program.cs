using Core.Entities.Business;
using Core.Enums;
using Core.Services;

static void Equal<T>(T expected, T actual, string name) where T : IEquatable<T>
{
    if (!expected.Equals(actual)) throw new Exception($"{name}: expected {expected}, got {actual}");
}
StudentAccount Row(int category, int type, double debit, double credit) => new() { Categoryid = category, TransactionTypeId = type, Debit = debit, Credit = credit };
var ledger = new List<StudentAccount>
{
    Row(10, 100, 1000, 0), Row(10, 800, 0, 100), Row(10, 1300, 0, 400),
    Row(20, 100, 2000, 0), Row(90, 100, 300, 0), Row(20, 1100, 0, 200),
    Row(20, 200, 0, 150), Row(20, 1000, 50, 0), Row(20, 810, 0, 25),
    Row(20, 1300, 0, 1000), Row(20, 1400, 100, 0)
};
var f = SchoolDashboardStatistics.Finance(ledger);
Equal(1000m, f.Previous, "Opening"); Equal(2100m, f.Dues, "Dues including tax and withdrawal");
Equal(100m, f.PreviousDiscount, "Opening settlement"); Equal(125m, f.Discount, "Discount reversal and settlement");
Equal(1300m, f.TotalPaid, "Net receipts with refunds");
Equal(ledger.Sum(SchoolDashboardStatistics.Net), f.Remaining, "Ledger reconciliation");
ledger.Add(new() { IsDeleted = true, Debit = 9000 });
Equal(f, SchoolDashboardStatistics.Finance(ledger), "Deleted entry excluded");
Equal(0m, SchoolDashboardStatistics.Finance([]).Remaining, "Empty ledger");
var id = Guid.NewGuid();
var contracts = new List<StudentContract> { new() { StudentId = id, StatusId = 10, StartDate = new(2026, 8, 10), EndDate = new(2027, 6, 1), YearId = 2026, RegisteredForYearId = 2026 } };
Equal(0, SchoolDashboardStatistics.ActiveAt(contracts, new(2026, 7, 31)), "Before start");
Equal(1, SchoolDashboardStatistics.ActiveAt(contracts, new(2026, 9, 30)), "New student remains active next month");
contracts[0].ActualEndDate = new(2026, 10, 10); contracts[0].StatusId = 30;
Equal(1, SchoolDashboardStatistics.ActiveAt(contracts, new(2026, 9, 30)), "Historical activity before withdrawal");
Equal(0, SchoolDashboardStatistics.ActiveAt(contracts, new(2026, 10, 31)), "Withdrawn excluded");
contracts[0].RegisteredForYearId = 2027;
Equal(0, SchoolDashboardStatistics.ActiveAt(contracts, new(2026, 9, 30)), "Future registration excluded");
Console.WriteLine("Dashboard checks passed (13 assertions).");
