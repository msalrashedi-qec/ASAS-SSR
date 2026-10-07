using Core.Entities.Business;

namespace Core.Services;

public sealed record MonthlyStudentCount(DateTime Month, int New, int Regular, int Withdrawn, int NextRegistered, int NextWithdrawn)
{
    public int Active => New + Regular - Withdrawn;
}

/// <summary>Groups saved contract statuses by contract dates and withdrawals by ActualEndDate.</summary>
public static class MonthlyStudentStatistics
{
    public static IReadOnlyList<MonthlyStudentCount> CalculateToCurrentMonth(List<StudentContract> contracts, int yearId, DateTime today)
    {
        var eligible = contracts.Where(c => !c.IsDeleted && c.YearId == yearId).ToList();
        var last = new DateTime(today.Year, today.Month, 1);
        if (eligible.Count == 0) return [];
        var firstDate = eligible.Min(c => c.StartDate);
        var first = new DateTime(firstDate.Year, firstDate.Month, 1);
        return first > last ? [] : Calculate(eligible, yearId, first, last);
    }

    public static IReadOnlyList<MonthlyStudentCount> Calculate(List<StudentContract> contracts, int yearId, DateTime from, DateTime to)
    {
        var first = new DateTime(from.Year, from.Month, 1);
        var last = new DateTime(to.Year, to.Month, 1);
        if (first > last || last.Year == 9999) throw new ArgumentException("Invalid month range.");
        contracts = contracts.Where(c => !c.IsDeleted && c.YearId == yearId).ToList();
        // Advance registrations belong to the year in which they were made, regardless of today's current year.
        var result = new List<MonthlyStudentCount>();
        for (var month = first; month <= last; month = month.AddMonths(1))
        {
            var end = month.AddMonths(1);
            bool InMonth(DateTime date) => date >= month && date < end;
            int Count(IEnumerable<StudentContract> values) => values.Select(x => x.StudentId).Distinct().Count();
            result.Add(new(month,
                Count(contracts.Where(c => c.StatusId == 10 && InMonth(c.StartDate))),
                Count(contracts.Where(c => c.StatusId == 20 && c.StartDate < end && c.EndDate >= month
                    && (!c.ActualEndDate.HasValue || c.ActualEndDate.Value >= month))),
                Count(contracts.Where(c => c.RegisteredForYearId <= yearId && c.ActualEndDate.HasValue && InMonth(c.ActualEndDate.Value))),
                Count(contracts.Where(c => c.StatusId == 5 && InMonth(c.StartDate))),
                Count(contracts.Where(c => c.RegisteredForYearId > yearId && c.ActualEndDate.HasValue && InMonth(c.ActualEndDate.Value)))));
        }
        return result;
    }
}
