using Core.Entities.Business;
using Core.Services;

StudentContract Contract(byte status, string start, string end = "2027-06-30", string? withdrawn = null, int registeredFor = 2026, int year = 2026) => new()
{
    Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), StatusId = status,
    StartDate = DateTime.Parse(start), EndDate = DateTime.Parse(end),
    ActualEndDate = withdrawn is null ? null : DateTime.Parse(withdrawn),
    YearId = year, RegisteredForYearId = registeredFor
};
void Equal<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new Exception($"{message}: expected {expected}, got {actual}");
}
var newcomer = Contract(10, "2026-08-31T23:59:59");
var duplicate = Contract(10, "2026-08-15");
duplicate.StudentId = newcomer.StudentId;
var deleted = Contract(10, "2026-08-01");
deleted.IsDeleted = true;
var contracts = new List<StudentContract>
{
    newcomer, duplicate, deleted,
    Contract(10, "2026-09-01"),
    Contract(20, "2026-07-01", "2026-09-01"),
    Contract(20, "2026-08-31", "2026-08-31"),
    Contract(20, "2026-06-01", "2026-07-31"),
    Contract(30, "2026-06-01", withdrawn: "2026-08-31T23:59:59"),
    Contract(30, "2026-07-01", withdrawn: "2026-09-01"),
    Contract(30, "2026-08-01"), // Missing withdrawal date must not become a guessed withdrawal.
    Contract(5, "2026-08-01", registeredFor: 2027),
    Contract(30, "2026-06-01", withdrawn: "2026-09-01", registeredFor: 2027),
    Contract(10, "2026-08-01", registeredFor: 2025, year: 2025),
    Contract(40, "2026-08-01") // Other statuses must not be treated as withdrawn.
};
var result = MonthlyStudentStatistics.Calculate(contracts, 2026, new(2026, 8, 15), new(2026, 10, 31));
Equal(3, result.Count, "Inclusive month range");
Equal(new MonthlyStudentCount(new(2026, 8, 1), 1, 2, 1, 1, 0), result[0], "August statuses and boundaries");
Equal(2, result[0].Active, "New + regular - withdrawn");
Equal(new MonthlyStudentCount(new(2026, 9, 1), 1, 1, 1, 0, 1), result[1], "September statuses and next-year withdrawal");
Equal(new MonthlyStudentCount(new(2026, 10, 1), 0, 0, 0, 0, 0), result[2], "Empty month");
var yearBoundary = MonthlyStudentStatistics.Calculate([], 2026, new(2026, 12, 1), new(2027, 1, 1));
Equal(new DateTime(2027, 1, 1), yearBoundary[1].Month, "Calendar-year boundary");
var onlyWithdrawal = MonthlyStudentStatistics.Calculate([Contract(30, "2026-07-01", withdrawn: "2026-08-01")], 2026, new(2026, 8, 1), new(2026, 8, 1));
Equal(-1, onlyWithdrawal[0].Active, "Preserve requested arithmetic without silently clamping");
try
{
    MonthlyStudentStatistics.Calculate([], 2026, new(2026, 9, 1), new(2026, 8, 1));
    throw new Exception("Reversed range was accepted");
}
catch (ArgumentException) { }
var automatic = MonthlyStudentStatistics.CalculateToCurrentMonth(contracts, 2026, new(2026, 9, 29));
Equal(new DateTime(2026, 6, 1), automatic[0].Month, "First contract month");
Equal(new DateTime(2026, 9, 1), automatic[^1].Month, "Current month included");
Equal(0, MonthlyStudentStatistics.CalculateToCurrentMonth([], 2026, new(2026, 9, 1)).Count, "No contracts");
Equal(0, MonthlyStudentStatistics.CalculateToCurrentMonth([Contract(10, "2026-10-01")], 2026, new(2026, 9, 1)).Count, "Future contracts");
var datedWithdrawals = MonthlyStudentStatistics.Calculate([
    Contract(20, "2026-07-01", withdrawn: "2026-08-15"),
    Contract(30, "2026-07-01", withdrawn: "2026-08-20", registeredFor: 2025),
    Contract(5, "2026-07-01", withdrawn: "2026-08-31", registeredFor: 2027)
], 2026, new(2026, 8, 1), new(2026, 9, 1));
Equal(2, datedWithdrawals[0].Withdrawn, "ActualEndDate determines withdrawal regardless of saved status");
Equal(1, datedWithdrawals[0].NextWithdrawn, "Advance withdrawal uses ActualEndDate");
Equal(0, datedWithdrawals[1].Withdrawn, "Withdrawal belongs only to its actual month");
Console.WriteLine("Monthly student statistics checks passed.");
