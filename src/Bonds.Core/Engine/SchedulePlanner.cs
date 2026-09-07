using System.Collections.Immutable;
using Bonds.Core.Domain;

namespace Bonds.Core.Engine;

/// <summary>
/// Builds the interest periods of one bond. Every boundary is counted from the
/// purchase date, never from the previous boundary. This keeps the day of the
/// month of the purchase as the anchor: a purchase on the 31st gives 30 September,
/// then 31 October, as annex 3 of the ROR letter shows.
/// </summary>
public static class SchedulePlanner
{
    public static ImmutableArray<InterestPeriod> Build(BondIssue issue, DateOnly purchase)
    {
        ArgumentNullException.ThrowIfNull(issue);
        issue.EnsureCanBuyOn(purchase);

        var periods = ImmutableArray.CreateBuilder<InterestPeriod>(issue.PeriodCount);
        for (var index = 0; index < issue.PeriodCount; index++)
        {
            periods.Add(new InterestPeriod(
                index,
                Boundary(purchase, index, issue.PeriodMonths),
                Boundary(purchase, index + 1, issue.PeriodMonths)));
        }

        return periods.MoveToImmutable();
    }

    /// <summary>
    /// The date that ends period <paramref name="index"/> minus one and starts
    /// period <paramref name="index"/>. <see cref="DateOnly.AddMonths"/> keeps the
    /// day of the month and shortens it only when the target month is shorter.
    /// </summary>
    private static DateOnly Boundary(DateOnly purchase, int index, int periodMonths) =>
        purchase.AddMonths(index * periodMonths);
}
