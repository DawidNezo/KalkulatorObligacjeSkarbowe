namespace Bonds.Core.Domain;

/// <summary>
/// One interest period. The first day belongs to the period and the last day does
/// not, which is how the issue letters count the days for the a/ACT ratio.
/// </summary>
public sealed record InterestPeriod(int Index, DateOnly Start, DateOnly End)
{
    /// <summary>ACT: the real number of days in the period.</summary>
    public int ActualDays => End.DayNumber - Start.DayNumber;

    /// <summary>
    /// a: the real number of days from the first day of the period, that day
    /// included, to <paramref name="day"/>, that day excluded.
    /// </summary>
    public int DaysAccruedTo(DateOnly day)
    {
        if (day < Start || day > End)
        {
            throw new ArgumentOutOfRangeException(nameof(day),
                $"Day {day:O} is outside interest period {Index} ({Start:O} to {End:O}).");
        }

        return day.DayNumber - Start.DayNumber;
    }

    /// <summary>True when <paramref name="day"/> settles this period, that is Start &lt; day &lt;= End.</summary>
    public bool Settles(DateOnly day) => day > Start && day <= End;
}
