using Bonds.Core.Domain;

namespace Bonds.Core.Engine;

/// <summary>One interest period together with the yearly rate that applies to it.</summary>
public sealed record PeriodRate(InterestPeriod Period, decimal AnnualRate, bool IsProjected);
