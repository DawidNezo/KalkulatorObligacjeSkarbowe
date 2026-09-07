using Bonds.Core.Calendar;
using Xunit;

namespace Bonds.Core.Tests;

public sealed class PolishBusinessDayCalendarTests
{
    private readonly PolishBusinessDayCalendar calendar = new();

    [Theory]
    [InlineData(2024, 3, 31)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    [InlineData(2027, 3, 28)]
    [InlineData(2038, 4, 25)]
    public void FindsEasterSunday(int year, int month, int day) =>
        Assert.Equal(new DateOnly(year, month, day), PolishBusinessDayCalendar.EasterSunday(year));

    [Theory]
    [InlineData(2026, 4, 6)]   // Poniedzialek Wielkanocny
    [InlineData(2026, 6, 4)]   // Boze Cialo
    [InlineData(2026, 1, 1)]
    [InlineData(2026, 1, 6)]
    [InlineData(2026, 5, 1)]
    [InlineData(2026, 5, 3)]
    [InlineData(2026, 8, 15)]
    [InlineData(2026, 11, 11)]
    [InlineData(2026, 12, 25)]
    [InlineData(2026, 12, 26)]
    public void HolidaysAreNotBusinessDays(int year, int month, int day) =>
        Assert.False(calendar.IsBusinessDay(new DateOnly(year, month, day)));

    [Theory]
    [InlineData(2025)]
    [InlineData(2026)]
    [InlineData(2030)]
    public void ChristmasEveIsAHolidayFromTwentyTwentyFive(int year) =>
        Assert.False(calendar.IsBusinessDay(new DateOnly(year, 12, 24)));

    [Fact]
    public void ChristmasEveWasABusinessDayBeforeTwentyTwentyFive() =>
        // Tuesday 24 December 2024; the act of 6 December 2024 (Dz.U. 2024
        // poz. 1965) makes the day a holiday from 2025 on.
        Assert.True(calendar.IsBusinessDay(new DateOnly(2024, 12, 24)));

    [Fact]
    public void CountsBackOverTheDecemberHolidays()
    {
        // From Thursday 1 January 2026 backwards, the business days are 31, 30,
        // 29, 23, 22, 19, 18, 17, 16 and 15 December 2025: the weekends fall out,
        // and so do 24, 25 and 26 December.
        var fixing = calendar.BusinessDaysBefore(new DateOnly(2026, 1, 1), 10);
        Assert.Equal(new DateOnly(2025, 12, 15), fixing);
    }

    [Theory]
    [InlineData(2026, 8, 22)]  // Saturday
    [InlineData(2026, 8, 23)]  // Sunday
    public void WeekendsAreNotBusinessDays(int year, int month, int day) =>
        Assert.False(calendar.IsBusinessDay(new DateOnly(year, month, day)));

    [Fact]
    public void OrdinaryWeekdayIsABusinessDay() =>
        Assert.True(calendar.IsBusinessDay(new DateOnly(2026, 8, 17)));

    [Fact]
    public void CountsTenBusinessDaysBackOverAWeekend()
    {
        // From Tuesday 1 September 2026 backwards: 31, 28, 27, 26, 25, 24, 21, 20, 19, 18 August.
        var fixing = calendar.BusinessDaysBefore(new DateOnly(2026, 9, 1), 10);
        Assert.Equal(new DateOnly(2026, 8, 18), fixing);
    }

    [Fact]
    public void CountsBackOverAHoliday()
    {
        // 15 August 2026 is a Saturday, thus only weekends fall out here.
        var fixing = calendar.BusinessDaysBefore(new DateOnly(2026, 5, 4), 1);
        Assert.Equal(new DateOnly(2026, 4, 30), fixing);
    }

    [Fact]
    public void ZeroBusinessDaysBackGivesTheSameDay() =>
        Assert.Equal(new DateOnly(2026, 8, 17), calendar.BusinessDaysBefore(new DateOnly(2026, 8, 17), 0));

    [Fact]
    public void RejectsANegativeCount() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => calendar.BusinessDaysBefore(new DateOnly(2026, 8, 17), -1));
}
