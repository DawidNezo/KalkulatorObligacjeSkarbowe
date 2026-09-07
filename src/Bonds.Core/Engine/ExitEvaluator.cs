using System.Collections.Immutable;

namespace Bonds.Core.Engine;

/// <summary>
/// Builds the month by month exit table of one bond. Month "m" means the date that
/// falls "m" months after the purchase, with the day of the month kept. For a bond
/// with monthly interest periods that date is a period boundary; for the other
/// bonds it falls inside a period and the a/ACT ratio covers the difference.
/// </summary>
public sealed class ExitEvaluator
{
    private readonly RedemptionCalculator redemption;
    private readonly TaxCalculator tax;

    public ExitEvaluator(RedemptionCalculator redemption, TaxCalculator tax)
    {
        ArgumentNullException.ThrowIfNull(redemption);
        ArgumentNullException.ThrowIfNull(tax);

        this.redemption = redemption;
        this.tax = tax;
    }

    /// <summary>
    /// One row per month, from the first month to one month past the end of the
    /// term. The last row shows that the bond no longer exists.
    /// </summary>
    public ImmutableArray<ExitOutcome> Evaluate(BondPlan plan, int units)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentOutOfRangeException.ThrowIfLessThan(units, 1);

        var rows = ImmutableArray.CreateBuilder<ExitOutcome>(plan.Issue.TermMonths + 1);
        for (var month = 1; month <= plan.Issue.TermMonths + 1; month++)
        {
            rows.Add(EvaluateMonth(plan, units, month));
        }

        return rows.MoveToImmutable();
    }

    private ExitOutcome EvaluateMonth(BondPlan plan, int units, int month)
    {
        var exitDate = plan.Purchase.AddMonths(month);

        if (month > plan.Issue.TermMonths)
        {
            return new ExitOutcome(month, exitDate, ExitKind.DoesNotExist, Details: null);
        }

        // One definition of the last day, shared with RedemptionCalculator.Settle.
        var isMaturity = exitDate == plan.Maturity;
        if (!isMaturity
            && !plan.Issue.EarlyRedemption.Allows(plan.Purchase, plan.Maturity, exitDate))
        {
            return new ExitOutcome(month, exitDate, ExitKind.NotAvailable, Details: null);
        }

        var gross = redemption.Settle(plan, exitDate);
        var couponsPerBond = redemption.CouponsPaidBefore(plan, gross.PeriodIndex);
        var settling = plan.Periods[gross.PeriodIndex];

        var details = new ExitDetails(
            gross.PeriodIndex,
            settling.AnnualRate,
            settling.IsProjected,
            couponsPerBond * units,
            gross.Interest * units,
            gross.Fee * units,
            gross.Payment * units,
            Settle(plan, units, gross, exitDate));

        return new ExitOutcome(
            month,
            exitDate,
            isMaturity ? ExitKind.Maturity : ExitKind.EarlyRedemption,
            details);
    }

    private ExitSettlement Settle(BondPlan plan, int units, GrossRedemption gross, DateOnly exitDate)
    {
        var flows = new List<CashFlow>
        {
            new(plan.Purchase, -(plan.Issue.PurchasePrice * units)),
        };

        var couponTax = Money.Zero;
        var couponsNet = Money.Zero;
        for (var index = 0; index < gross.PeriodIndex; index++)
        {
            var coupon = redemption.Coupon(plan, index) * units;
            if (coupon == Money.Zero)
            {
                continue;
            }

            var withheld = tax.Withhold(coupon);
            couponTax += withheld;
            couponsNet += coupon - withheld;
            flows.Add(new CashFlow(plan.Periods[index].Period.End, coupon - withheld));
        }

        // The fee lowers the taxable interest, thus a fee that swallows the whole
        // interest leaves no tax to withhold.
        var taxableInterest = Money.Max(Money.Zero, gross.Interest - gross.Fee) * units;
        var redemptionTax = tax.Withhold(taxableInterest);
        var redemptionNet = (gross.Payment * units) - redemptionTax;
        flows.Add(new CashFlow(exitDate, redemptionNet));

        var cost = plan.Issue.PurchasePrice * units;
        var netProfit = couponsNet + redemptionNet - cost;

        return new ExitSettlement(
            couponTax,
            redemptionTax,
            couponsNet,
            redemptionNet,
            netProfit,
            decimal.Round(netProfit.Zloty / cost.Zloty, 6),
            InternalRateOfReturn.Annualised(flows));
    }
}
