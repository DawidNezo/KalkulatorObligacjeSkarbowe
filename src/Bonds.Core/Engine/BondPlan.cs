using System.Collections.Immutable;
using Bonds.Core.Domain;
using Bonds.Core.Market;

namespace Bonds.Core.Engine;

/// <summary>
/// One bond bought on one date, with its schedule and every rate resolved once.
/// The exit table asks this object many questions, thus the rates are resolved
/// here and not for each row.
/// </summary>
public sealed class BondPlan
{
    private BondPlan(BondIssue issue, DateOnly purchase, ImmutableArray<PeriodRate> periods)
    {
        Issue = issue;
        Purchase = purchase;
        Periods = periods;
    }

    public BondIssue Issue { get; }

    public DateOnly Purchase { get; }

    public ImmutableArray<PeriodRate> Periods { get; }

    public DateOnly Maturity => Periods[^1].Period.End;

    /// <summary>The first period whose rate is an assumption and not published data.</summary>
    public PeriodRate? FirstProjectedPeriod => Periods.FirstOrDefault(p => p.IsProjected);

    public static BondPlan Create(BondIssue issue, DateOnly purchase, IMarketData market)
    {
        ArgumentNullException.ThrowIfNull(issue);
        ArgumentNullException.ThrowIfNull(market);

        var schedule = SchedulePlanner.Build(issue, purchase);
        var rates = ImmutableArray.CreateBuilder<PeriodRate>(schedule.Length);
        foreach (var period in schedule)
        {
            var quote = issue.Rate.RateFor(period.Index, period.Start, market);
            rates.Add(new PeriodRate(period, quote.Value, quote.IsProjected));
        }

        return new BondPlan(issue, purchase, rates.MoveToImmutable());
    }

    /// <summary>
    /// The period that a payment on <paramref name="day"/> settles, that is the
    /// period with Start &lt; day &lt;= End. The purchase date itself settles nothing.
    /// </summary>
    public PeriodRate PeriodSettling(DateOnly day)
    {
        foreach (var period in Periods)
        {
            if (period.Period.Settles(day))
            {
                return period;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(day),
            $"Bond {Issue.Code} bought on {Purchase:yyyy-MM-dd} has no interest period that settles {day:yyyy-MM-dd}.");
    }
}
