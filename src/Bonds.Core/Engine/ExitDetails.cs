namespace Bonds.Core.Engine;

/// <summary>
/// The figures of an exit that really can happen. Every amount covers the whole
/// holding. The gross amounts include no tax; <see cref="Settlement"/> holds the
/// after-tax result.
/// </summary>
public sealed record ExitDetails(
    int PeriodIndex,
    decimal PeriodRate,
    bool RateIsProjected,
    Money CouponsGross,
    Money RedemptionInterestGross,
    Money Fee,
    Money RedemptionGross,
    ExitSettlement Settlement)
{
    /// <summary>Every zloty of interest that the bond earned up to the exit.</summary>
    public Money TotalInterestGross => CouponsGross + RedemptionInterestGross;

    /// <summary>Everything that the holder receives before tax, the face value included.</summary>
    public Money TotalReceiptsGross => CouponsGross + RedemptionGross;
}
