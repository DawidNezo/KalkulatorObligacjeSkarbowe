namespace Bonds.Core.Domain;

/// <summary>
/// The amount "b" that an early redemption takes off the payment. The letters use
/// three different structures, thus each one is a separate rule.
/// </summary>
public abstract record FeeRule
{
    /// <summary>
    /// Returns the amount to take off.
    /// <paramref name="periodIndex"/> is zero for the first interest period.
    /// <paramref name="accruedInterest"/> is the whole interest that the bond
    /// earned and did not pay out yet.
    /// </summary>
    public abstract Money Compute(int periodIndex, Money accruedInterest);
}

/// <summary>
/// In the first interest period the fee cannot exceed the interest earned. From
/// the second period the full fee always applies, thus the payment can fall below
/// the face value. ROR, DOR and COI.
/// </summary>
public sealed record TwoTierFee(Money Fee) : FeeRule
{
    public override Money Compute(int periodIndex, Money accruedInterest) =>
        periodIndex == 0 ? Money.Min(accruedInterest, Fee) : Fee;
}

/// <summary>
/// The fee never exceeds the interest earned, in any interest period. TOS, EDO,
/// ROS and ROD, which hold the interest until the redemption.
/// </summary>
public sealed record AccruedCappedFee(Money Fee) : FeeRule
{
    public override Money Compute(int periodIndex, Money accruedInterest) =>
        Money.Min(accruedInterest, Fee);
}

/// <summary>
/// The whole interest is lost and the redemption pays the face value. OTS, whose
/// letter states that no interest is due on an early redemption.
/// </summary>
public sealed record ForfeitAllInterestFee : FeeRule
{
    public override Money Compute(int periodIndex, Money accruedInterest) => accruedInterest;
}
