using System.Text.Json;
using System.Text.Json.Serialization;
using Bonds.Core.Engine;

namespace Bonds.Core.Reporting;

/// <summary>
/// The whole result as JSON, for further work in another tool. Amounts appear both
/// in grosze, which is exact, and in zloty, which is easy to read.
/// </summary>
public sealed class JsonReportWriter : IReportWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public string Format => "json";

    public string Render(CalculationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var payload = new
        {
            purchase = report.Purchase,
            units = report.Units,
            taxRate = report.TaxRate,
            scenario = new
            {
                name = report.Scenario.Name,
                assumedCpi = report.Scenario.AssumedCpi,
                assumedNbpReference = report.Scenario.AssumedNbpReference,
            },
            bonds = report.Bonds.Select(bond => new
            {
                code = bond.Plan.Issue.Code,
                kind = bond.Plan.Issue.Kind.ToString(),
                termMonths = bond.Plan.Issue.TermMonths,
                maturity = bond.Plan.Maturity,
                source = bond.Plan.Issue.Source,
                periods = bond.Plan.Periods.Select(period => new
                {
                    index = period.Period.Index,
                    start = period.Period.Start,
                    end = period.Period.End,
                    actualDays = period.Period.ActualDays,
                    annualRate = period.AnnualRate,
                    isProjected = period.IsProjected,
                }),
                exits = bond.Exits.Select(Describe),
            }),
        };

        return JsonSerializer.Serialize(payload, Options);
    }

    private static object Describe(ExitOutcome exit) => new
    {
        month = exit.MonthIndex,
        date = exit.ExitDate,
        kind = exit.Kind.ToString(),
        details = exit.Details is null ? null : new
        {
            periodIndex = exit.Details.PeriodIndex,
            periodRate = exit.Details.PeriodRate,
            rateIsProjected = exit.Details.RateIsProjected,
            couponsGrossGrosze = exit.Details.CouponsGross.Grosze,
            redemptionInterestGrossGrosze = exit.Details.RedemptionInterestGross.Grosze,
            feeGrosze = exit.Details.Fee.Grosze,
            redemptionGrossGrosze = exit.Details.RedemptionGross.Grosze,
            totalReceiptsGrossZloty = exit.Details.TotalReceiptsGross.Zloty,
            settlement = Describe(exit.Details.Settlement),
        },
    };

    private static object Describe(ExitSettlement settlement) => new
    {
        couponTaxGrosze = settlement.CouponTax.Grosze,
        redemptionTaxGrosze = settlement.RedemptionTax.Grosze,
        couponsNetGrosze = settlement.CouponsNet.Grosze,
        redemptionNetGrosze = settlement.RedemptionNet.Grosze,
        netProfitGrosze = settlement.NetProfit.Grosze,
        netProfitZloty = settlement.NetProfit.Zloty,
        netProfitRate = settlement.NetProfitRate,
        annualisedNetReturn = settlement.AnnualisedNetReturn,
    };
}
