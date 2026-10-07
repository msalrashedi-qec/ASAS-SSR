namespace Core.Services;

public static class ReceiptAllocation
{
    public static bool IsValid(IEnumerable<double> amounts, decimal originalTotal)
    {
        var values = amounts.ToList();
        if (values.Count == 0 || originalTotal <= 0) return false;
        decimal total = 0;
        foreach (var value in values)
        {
            if (!double.IsFinite(value) || value < 0 || value >= (double)decimal.MaxValue) return false;
            var amount = (decimal)value;
            if (amount != decimal.Round(amount, 2)) return false;
            if (amount > originalTotal - total) return false;
            total += amount;
        }
        return total == originalTotal;
    }

    public static bool MatchesLedger(
        IEnumerable<(byte Semester, int Service, int Category, double Amount)> details,
        IEnumerable<(byte Semester, int Service, int Category, double Amount)> accounts)
    {
        var expected = details.GroupBy(x => (x.Semester, x.Service, x.Category))
            .ToDictionary(x => x.Key, x => x.Sum(a => (decimal)a.Amount));
        var actual = accounts.GroupBy(x => (x.Semester, x.Service, x.Category))
            .ToDictionary(x => x.Key, x => x.Sum(a => (decimal)a.Amount));
        return expected.Count == actual.Count && expected.All(x => actual.TryGetValue(x.Key, out var amount) && amount == x.Value);
    }
}
