using Bonds.Core.Market;

namespace Bonds.Core.Tests.Support;

/// <summary>
/// Market data that always answers with the same rate. It lets an engine test
/// state the rate that it wants and read the result, with no data file involved.
/// It records the raw arguments of every question, thus a test can assert what
/// the rule asked for without running the production calendar.
/// </summary>
internal sealed class FixedMarketData : IMarketData
{
    private readonly decimal nbpReference;
    private readonly decimal cpi;
    private readonly bool projected;

    internal FixedMarketData(decimal nbpReference, decimal cpi, bool projected = false)
    {
        this.nbpReference = nbpReference;
        this.cpi = cpi;
        this.projected = projected;
    }

    internal int NbpLookups { get; private set; }

    internal (DateOnly FirstDayOfMonth, int BusinessDaysBefore)? LastNbpRequest { get; private set; }

    internal YearMonth? LastCpiPublicationMonth { get; private set; }

    public RateQuote NbpReferenceOn(DateOnly firstDayOfMonth, int businessDaysBefore)
    {
        NbpLookups++;
        LastNbpRequest = (firstDayOfMonth, businessDaysBefore);
        return new RateQuote(nbpReference, projected);
    }

    public RateQuote CpiPublishedIn(YearMonth publicationMonth)
    {
        LastCpiPublicationMonth = publicationMonth;
        return new RateQuote(cpi, projected);
    }
}
