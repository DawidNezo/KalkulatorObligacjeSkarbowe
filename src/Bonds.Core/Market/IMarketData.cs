namespace Bonds.Core.Market;

/// <summary>Gives the published market data, or the assumption that stands in for it.</summary>
public interface IMarketData
{
    /// <summary>
    /// The NBP reference rate in force <paramref name="businessDaysBefore"/> business
    /// days before <paramref name="firstDayOfMonth"/>.
    /// </summary>
    RateQuote NbpReferenceOn(DateOnly firstDayOfMonth, int businessDaysBefore);

    /// <summary>The yearly CPI growth that Statistics Poland published in the month given.</summary>
    RateQuote CpiPublishedIn(YearMonth publicationMonth);
}
