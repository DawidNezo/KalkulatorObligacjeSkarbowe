using Bonds.Core.Calendar;

namespace Bonds.Core.Market;

/// <summary>
/// Published market data, with a single assumed value for every date beyond it.
/// The assumed values come from the scenario that the user chooses.
/// </summary>
public sealed class MarketData : IMarketData
{
    private readonly DatedRateSeries nbpReference;
    private readonly MonthlyRateSeries cpi;
    private readonly IBusinessDayCalendar calendar;
    private readonly decimal assumedNbpReference;
    private readonly decimal assumedCpi;

    public MarketData(
        DatedRateSeries nbpReference,
        MonthlyRateSeries cpi,
        IBusinessDayCalendar calendar,
        decimal assumedNbpReference,
        decimal assumedCpi)
    {
        ArgumentNullException.ThrowIfNull(nbpReference);
        ArgumentNullException.ThrowIfNull(cpi);
        ArgumentNullException.ThrowIfNull(calendar);

        this.nbpReference = nbpReference;
        this.cpi = cpi;
        this.calendar = calendar;
        this.assumedNbpReference = assumedNbpReference;
        this.assumedCpi = assumedCpi;
    }

    public RateQuote NbpReferenceOn(DateOnly firstDayOfMonth, int businessDaysBefore)
    {
        var fixingDate = calendar.BusinessDaysBefore(firstDayOfMonth, businessDaysBefore);
        var published = nbpReference.ValueOn(fixingDate);
        return published is null
            ? new RateQuote(assumedNbpReference, IsProjected: true)
            : new RateQuote(published.Value, IsProjected: false);
    }

    public RateQuote CpiPublishedIn(YearMonth publicationMonth)
    {
        var published = cpi.ValueIn(publicationMonth);
        return published is null
            ? new RateQuote(assumedCpi, IsProjected: true)
            : new RateQuote(published.Value, IsProjected: false);
    }
}
