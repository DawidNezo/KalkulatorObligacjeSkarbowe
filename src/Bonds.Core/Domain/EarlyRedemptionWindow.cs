namespace Bonds.Core.Domain;

/// <summary>
/// The period in which the holder can call for an early redemption. The letters
/// count both limits in calendar days.
/// </summary>
public sealed record EarlyRedemptionWindow(int MinDaysAfterPurchase, int MinDaysBeforeMaturity)
{
    public bool Allows(DateOnly purchase, DateOnly maturity, DateOnly redemption) =>
        redemption.DayNumber - purchase.DayNumber > MinDaysAfterPurchase
        && maturity.DayNumber - redemption.DayNumber >= MinDaysBeforeMaturity;
}
