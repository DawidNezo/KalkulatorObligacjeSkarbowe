using System.Collections.Concurrent;

namespace Bonds.Core.Calendar;

/// <summary>
/// Polish business days. A business day is a day that is not a Saturday, not a
/// Sunday and not a public holiday. The issue letters state that Saturday is not
/// a business day.
/// </summary>
public sealed class PolishBusinessDayCalendar : IBusinessDayCalendar
{
    public static readonly PolishBusinessDayCalendar Instance = new();

    private static readonly (int Month, int Day)[] FixedHolidays =
    [
        (1, 1),   // Nowy Rok
        (1, 6),   // Trzech Kroli
        (5, 1),   // Swieto Pracy
        (5, 3),   // Swieto Konstytucji 3 Maja
        (8, 15),  // Wniebowziecie Najswietszej Maryi Panny
        (11, 1),  // Wszystkich Swietych
        (11, 11), // Narodowe Swieto Niepodleglosci
        (12, 25), // pierwszy dzien Bozego Narodzenia
        (12, 26), // drugi dzien Bozego Narodzenia
    ];

    /// <summary>
    /// Wigilia (24 December) is a public holiday from 2025 on (act of 6 December
    /// 2024, Dz.U. 2024 poz. 1965). Earlier years must keep it a business day, or
    /// every historic December fixing moves by one day.
    /// </summary>
    private const int FirstYearWithChristmasEveHoliday = 2025;

    private const int EasterMondayOffset = 1;
    private const int CorpusChristiOffset = 60;

    private readonly ConcurrentDictionary<int, HashSet<DateOnly>> movableByYear = [];

    public bool IsBusinessDay(DateOnly date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        foreach (var (month, day) in FixedHolidays)
        {
            if (date.Month == month && date.Day == day)
            {
                return false;
            }
        }

        if (date is { Month: 12, Day: 24 } && date.Year >= FirstYearWithChristmasEveHoliday)
        {
            return false;
        }

        return !MovableHolidays(date.Year).Contains(date);
    }

    public DateOnly BusinessDaysBefore(DateOnly date, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var result = date;
        for (var counted = 0; counted < count;)
        {
            result = result.AddDays(-1);
            if (IsBusinessDay(result))
            {
                counted++;
            }
        }

        return result;
    }

    private HashSet<DateOnly> MovableHolidays(int year) =>
        movableByYear.GetOrAdd(year, static y =>
        {
            var easter = EasterSunday(y);
            return
            [
                easter,
                easter.AddDays(EasterMondayOffset),
                easter.AddDays(CorpusChristiOffset),
            ];
        });

    /// <summary>Gauss and Meeus algorithm for the Gregorian date of Easter Sunday.</summary>
    internal static DateOnly EasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = ((19 * a) + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + (2 * e) + (2 * i) - h - k) % 7;
        var m = (a + (11 * h) + (22 * l)) / 451;
        var month = (h + l - (7 * m) + 114) / 31;
        var day = ((h + l - (7 * m) + 114) % 31) + 1;
        return new DateOnly(year, month, day);
    }
}
