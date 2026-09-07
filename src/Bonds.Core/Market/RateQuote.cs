namespace Bonds.Core.Market;

/// <summary>
/// A yearly rate, and whether it comes from published data or from an assumption.
/// A report must show which of its numbers are projections.
/// </summary>
public readonly record struct RateQuote(decimal Value, bool IsProjected);
