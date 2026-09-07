namespace Bonds.Core.Calendar;

/// <summary>
/// Tells business days from non-business days. The variable interest rate rules
/// count business days backwards from the first day of a calendar month.
/// </summary>
public interface IBusinessDayCalendar
{
    bool IsBusinessDay(DateOnly date);

    /// <summary>
    /// Returns the date that is <paramref name="count"/> business days before
    /// <paramref name="date"/>. The day given is never counted.
    /// </summary>
    DateOnly BusinessDaysBefore(DateOnly date, int count);
}
