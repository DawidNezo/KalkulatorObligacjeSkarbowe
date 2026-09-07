using System.Diagnostics.CodeAnalysis;
using Bonds.Core.Domain;

namespace Bonds.Core.Engine;

/// <summary>
/// The formulas of the issue letters. One expression covers all eight bond
/// families:
/// <code>
/// WP = N * PROD(1 + r_j, j &lt; k) * (1 + r_k * a / (ACT * F)) - b
/// </code>
/// The product is one for the bonds that pay their coupons out. The redemption at
/// the end of the term uses the full product with no rounding in between, as
/// annex 1 of the TOS letter and annex 2 of the EDO, ROS and ROD letters state.
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static",
    Justification = "These methods hold no state today. They stay instance methods because the class is injected as a collaborator, and a static method cannot be substituted in a test nor replaced by another implementation.")]
public sealed class RedemptionCalculator
{
    /// <summary>The coupon of one whole interest period: N * r / F.</summary>
    public Money Coupon(BondPlan plan, int periodIndex)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (!plan.Issue.PaysCoupons)
        {
            return Money.Zero;
        }

        var rate = plan.Periods[periodIndex].AnnualRate;
        return Money.RoundToGrosz(plan.Issue.Nominal * (rate / plan.Issue.PeriodsPerYear));
    }

    /// <summary>The sum of the coupons that the holder received before the day given.</summary>
    public Money CouponsPaidBefore(BondPlan plan, int settlingPeriodIndex)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var total = Money.Zero;
        for (var index = 0; index < settlingPeriodIndex; index++)
        {
            total += Coupon(plan, index);
        }

        return total;
    }

    /// <summary>
    /// What one bond pays on <paramref name="day"/>. The day of the maturity gives
    /// the ordinary redemption with no fee; every earlier day gives an early
    /// redemption.
    /// </summary>
    public GrossRedemption Settle(BondPlan plan, DateOnly day)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var settling = plan.PeriodSettling(day);
        var isMaturity = day == plan.Maturity;
        var nominal = plan.Issue.Nominal;

        var bondValue = isMaturity
            ? MaturityValue(plan)
            : Money.RoundToGrosz(CapitalisedBase(plan, settling.Period.Index) * (1m + PartialFactor(plan, settling, day)));

        var interest = bondValue - nominal;
        var fee = isMaturity ? Money.Zero : plan.Issue.Fee.Compute(settling.Period.Index, interest);
        var payment = ApplyFloor(plan.Issue, settling.Period.Index, bondValue - fee, nominal);

        return new GrossRedemption(settling.Period.Index, bondValue, interest, fee, payment);
    }

    /// <summary>The value of one bond at the end of its term, before tax.</summary>
    public Money MaturityValue(BondPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (!plan.Issue.Capitalizes)
        {
            return plan.Issue.Nominal + Coupon(plan, plan.Periods.Length - 1);
        }

        var product = 1m;
        foreach (var period in plan.Periods)
        {
            product *= 1m + period.AnnualRate;
        }

        return Money.RoundToGrosz(plan.Issue.Nominal * product);
    }

    /// <summary>
    /// "N_k-1": the face value grown by the interest of every earlier period. The
    /// TOS letter rounds this base to a grosz; the EDO, ROS and ROD letters do not.
    /// </summary>
    private static decimal CapitalisedBase(BondPlan plan, int settlingPeriodIndex)
    {
        if (!plan.Issue.Capitalizes)
        {
            return plan.Issue.Nominal.Zloty;
        }

        var product = 1m;
        for (var index = 0; index < settlingPeriodIndex; index++)
        {
            product *= 1m + plan.Periods[index].AnnualRate;
        }

        var amount = plan.Issue.Nominal * product;
        return plan.Issue.RoundCapitalizedBase ? Money.RoundToGrosz(amount).Zloty : amount;
    }

    /// <summary>"r_k * a / (ACT * F)": the part of the period that already passed.</summary>
    private static decimal PartialFactor(BondPlan plan, PeriodRate settling, DateOnly day)
    {
        var accruedDays = settling.Period.DaysAccruedTo(day);
        var divisor = settling.Period.ActualDays * plan.Issue.PeriodsPerYear;
        return settling.AnnualRate * accruedDays / divisor;
    }

    private static Money ApplyFloor(BondIssue issue, int periodIndex, Money payment, Money nominal) =>
        issue.Floor switch
        {
            PrincipalFloor.AllPeriods => Money.Max(payment, nominal),
            PrincipalFloor.FirstPeriodOnly when periodIndex == 0 => Money.Max(payment, nominal),
            _ => payment,
        };
}
