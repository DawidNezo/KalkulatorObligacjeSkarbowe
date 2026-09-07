namespace Bonds.Core.Engine;

/// <summary>One dated payment. The purchase is negative and every receipt is positive.</summary>
public readonly record struct CashFlow(DateOnly Date, Money Amount);
