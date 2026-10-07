namespace Web.Extensions;

public static class ReportFormatter
{
    private static readonly CultureInfo ReportCulture = CultureInfo.InvariantCulture;

    public static string Amount(double value)
    {
        var rounded = Math.Round(value, 2);
        return rounded == Math.Truncate(rounded)
            ? rounded.ToString("N0", ReportCulture)
            : rounded.ToString("N2", ReportCulture);
    }

    public static string Amount(decimal value)
    {
        var rounded = Math.Round(value, 2);
        return rounded == Math.Truncate(rounded)
            ? rounded.ToString("N0", ReportCulture)
            : rounded.ToString("N2", ReportCulture);
    }

    public static string Date(DateTime value) => value.ToString("dd/MM/yyyy", ReportCulture);

    public static string DateTime(DateTime value) => value.ToString("dd/MM/yyyy hh:mm tt", ReportCulture);

    public static string Time(DateTime value) => value.ToString("hh:mm tt", ReportCulture);
}
