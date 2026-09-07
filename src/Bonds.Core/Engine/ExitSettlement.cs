namespace Bonds.Core.Engine;

/// <summary>
/// The after-tax result of one exit. Every amount covers the whole holding, thus
/// a holding of one bond gives the amounts for one bond. The tax is withheld from
/// each payment on the whole holding, which is what the issue agent does.
/// </summary>
public sealed record ExitSettlement(
    Money CouponTax,
    Money RedemptionTax,
    Money CouponsNet,
    Money RedemptionNet,
    Money NetProfit,
    decimal NetProfitRate,
    decimal? AnnualisedNetReturn)
{
    public Money TotalTax => CouponTax + RedemptionTax;
}
