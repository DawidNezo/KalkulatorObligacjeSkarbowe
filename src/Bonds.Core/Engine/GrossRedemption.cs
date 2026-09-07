namespace Bonds.Core.Engine;

/// <summary>
/// What one bond pays on a given day, before tax. <see cref="Payment"/> is the
/// amount "WP" of the issue letters.
/// </summary>
public sealed record GrossRedemption(
    int PeriodIndex,
    Money BondValue,
    Money Interest,
    Money Fee,
    Money Payment);
