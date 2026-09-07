namespace Bonds.Core.Engine;

/// <summary>
/// The flat tax on interest income, known as "podatek Belki". The issue letters
/// say nothing about tax. The rate comes from art. 30a ust. 1 pkt 2 of the
/// Personal Income Tax Act. Art. 63 par. 1a of the Tax Ordinance Act rounds the
/// tax on interest and discount from securities up to a full grosz; the
/// whole-zloty rule of art. 63 par. 1 does not apply to this tax.
/// </summary>
public sealed class TaxCalculator
{
    /// <summary>The standard flat rate on interest income.</summary>
    public const decimal StandardRate = 0.19m;

    public TaxCalculator(decimal rate)
    {
        if (rate < 0m || rate > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), "Stawka podatku musi być między 0 a 1.");
        }

        Rate = rate;
    }

    public decimal Rate { get; }

    /// <summary>
    /// The tax to withhold from one payment: the rate applied to the base, rounded
    /// up to a grosz. A base at or below zero gives no tax, because a loss on one
    /// bond gives no refund.
    /// </summary>
    public Money Withhold(Money taxableBase) =>
        taxableBase <= Money.Zero ? Money.Zero : Money.CeilToGrosz(taxableBase * Rate);
}
