namespace Bonds.Core.Data;

/// <summary>
/// The values that stand in for market data that nobody published yet. A ten-year
/// or twelve-year bond takes most of its rates from here, thus the report must
/// name the scenario that it used.
/// </summary>
public sealed record Scenario(string Name, decimal AssumedCpi, decimal AssumedNbpReference);
