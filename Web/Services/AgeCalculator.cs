namespace Web.Services
{
    /// <summary>
    /// Represents a precise age breakdown in years, months, and days.
    /// </summary>
    public class AgeResult
    {
        public int Years  { get; init; }
        public int Months { get; init; }
        public int Days   { get; init; }

        public int TotalDays { get; init; }

        public override string ToString() => $"{Years} {(Years == 1 ? "سنة" : "سنوات")} , " + $"{Months} {(Months == 1 ? "شهر" : "أشهر")} , " + $"{Days} يوم";
    }

    /// <summary>
    /// Provides accurate age calculation in years, months, and days.
    /// </summary>
    public static class AgeCalculator
    {
        /// <summary>
        /// Calculates the age from a date of birth up to today.
        /// </summary>
        /// <param name="dateOfBirth">The student's date of birth.</param>
        /// <returns>An <see cref="AgeResult"/> with years, months, and days.</returns>
        public static AgeResult Calculate(DateTime dateOfBirth) => Calculate(dateOfBirth, DateTime.Today);

        /// <summary>
        /// Calculates the age from a date of birth up to a specific reference date.
        /// </summary>
        /// <param name="dateOfBirth">The student's date of birth.</param>
        /// <param name="referenceDate">The date to calculate the age against.</param>
        /// <returns>An <see cref="AgeResult"/> with years, months, and days.</returns>
        /// <exception cref="ArgumentException">Thrown when dateOfBirth is in the future.</exception>
        public static AgeResult Calculate(DateTime dateOfBirth, DateTime referenceDate)
        {
            // Normalize to date-only (ignore time component)
            var dob = dateOfBirth.Date;
            var today = referenceDate.Date;

            if (dob > today)
                throw new ArgumentException("Date of birth cannot be in the future.", nameof(dateOfBirth));

            int years  = today.Year  - dob.Year;
            int months = today.Month - dob.Month;
            int days   = today.Day   - dob.Day;

            // Borrow days from the previous month if needed
            if (days < 0)
            {
                months--;
                // Get the number of days in the previous month relative to 'today'
                int prevMonth = today.Month == 1 ? 12 : today.Month - 1;
                int prevYear  = today.Month == 1 ? today.Year - 1 : today.Year;
                days += DateTime.DaysInMonth(prevYear, prevMonth);
            }

            // Borrow months from the previous year if needed
            if (months < 0)
            {
                years--;
                months += 12;
            }

            int totalDays = (today - dob).Days;

            return new AgeResult
            {
                Years     = years,
                Months    = months,
                Days      = days,
                TotalDays = totalDays
            };
        }
    }
}
