namespace Bonds.Core.Engine;

/// <summary>
/// The yearly internal rate of return of dated payments. It is the only fair way
/// to compare a bond that pays a coupon every month against one that pays nothing
/// until the end of its term.
/// </summary>
public static class InternalRateOfReturn
{
    private const decimal LowestRate = -0.999999m;
    private const decimal HighestRate = 10m;
    private const int Iterations = 100;
    private const int DaysPerYear = 365;

    /// <summary>
    /// Returns the yearly rate that brings the present value of the payments to
    /// zero, or null when no rate in the search range does.
    /// </summary>
    public static decimal? Annualised(IReadOnlyList<CashFlow> flows)
    {
        ArgumentNullException.ThrowIfNull(flows);

        if (flows.Count < 2)
        {
            return null;
        }

        var start = flows[0].Date;
        var low = LowestRate;
        var high = HighestRate;
        var valueAtLow = PresentValue(flows, start, low);
        var valueAtHigh = PresentValue(flows, start, high);

        if (double.IsNaN(valueAtLow) || double.IsNaN(valueAtHigh) || valueAtLow * valueAtHigh > 0)
        {
            return null;
        }

        for (var step = 0; step < Iterations; step++)
        {
            var middle = (low + high) / 2m;
            if (PresentValue(flows, start, middle) * valueAtLow > 0)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return decimal.Round((low + high) / 2m, 6);
    }

    /// <summary>
    /// Discounts the payments. The exponent is not a whole number, thus this step
    /// uses <see cref="double"/>; the result is a comparison figure and never money.
    /// </summary>
    private static double PresentValue(IReadOnlyList<CashFlow> flows, DateOnly start, decimal rate)
    {
        var discountBase = 1d + (double)rate;
        var total = 0d;
        foreach (var flow in flows)
        {
            var years = (flow.Date.DayNumber - start.DayNumber) / (double)DaysPerYear;
            total += (double)flow.Amount.Zloty / Math.Pow(discountBase, years);
        }

        return total;
    }
}
