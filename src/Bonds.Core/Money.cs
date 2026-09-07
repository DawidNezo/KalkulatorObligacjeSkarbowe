namespace Bonds.Core;

/// <summary>
/// An amount of Polish zloty. The amount is an integer number of grosze, thus
/// no binary floating-point error can occur. Multiplication by a rate gives a
/// <see cref="decimal"/>, because the caller must decide where to round.
/// </summary>
public readonly record struct Money : IComparable<Money>, IFormattable
{
    /// <summary>Grosze in one zloty.</summary>
    public const int GroszePerZloty = 100;

    private const decimal GroszePerZlotyDecimal = GroszePerZloty;

    public static readonly Money Zero = new(0);

    private Money(long grosze) => Grosze = grosze;

    /// <summary>The amount as a whole number of grosze. Can be negative.</summary>
    public long Grosze { get; }

    /// <summary>The amount in zloty. Always exact, because grosze are integers.</summary>
    public decimal Zloty => Grosze / GroszePerZlotyDecimal;

    public bool IsNegative => Grosze < 0;

    public static Money FromGrosze(long grosze) => new(grosze);

    /// <summary>
    /// Rounds an amount in zloty to the nearest grosz. Halves go away from zero,
    /// which is the rule that the issue letters use for their two-decimal amounts.
    /// </summary>
    public static Money RoundToGrosz(decimal zloty) =>
        new(decimal.ToInt64(Math.Round(zloty * GroszePerZlotyDecimal, 0, MidpointRounding.AwayFromZero)));

    /// <summary>
    /// Rounds an amount in zloty up to a grosz. Art. 63 par. 1a of the Tax
    /// Ordinance Act rounds the flat tax on interest from securities this way.
    /// </summary>
    public static Money CeilToGrosz(decimal zloty) =>
        new(decimal.ToInt64(Math.Ceiling(zloty * GroszePerZlotyDecimal)));

    public static Money Min(Money a, Money b) => a.Grosze <= b.Grosze ? a : b;

    public static Money Max(Money a, Money b) => a.Grosze >= b.Grosze ? a : b;

    public static Money operator +(Money a, Money b) => new(a.Grosze + b.Grosze);

    public static Money operator -(Money a, Money b) => new(a.Grosze - b.Grosze);

    public static Money operator -(Money a) => new(-a.Grosze);

    /// <summary>Scales the amount by a whole factor, for example a number of bonds.</summary>
    public static Money operator *(Money a, int factor) => new(a.Grosze * factor);

    /// <summary>
    /// Applies a rate. The result is a <see cref="decimal"/> and not a
    /// <see cref="Money"/>, because only the caller knows how to round it.
    /// </summary>
    public static decimal operator *(Money a, decimal rate) => a.Zloty * rate;

    public static bool operator <(Money a, Money b) => a.Grosze < b.Grosze;

    public static bool operator >(Money a, Money b) => a.Grosze > b.Grosze;

    public static bool operator <=(Money a, Money b) => a.Grosze <= b.Grosze;

    public static bool operator >=(Money a, Money b) => a.Grosze >= b.Grosze;

    public int CompareTo(Money other) => Grosze.CompareTo(other.Grosze);

    public override string ToString() => ToString(null, null);

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Zloty.ToString(format ?? "0.00", formatProvider ?? System.Globalization.CultureInfo.InvariantCulture);
}
