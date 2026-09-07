using System.Collections.Immutable;
using Bonds.Core.Data;
using Bonds.Core.Engine;

namespace Bonds.Core.Reporting;

/// <summary>One bond issue with its month by month exit table.</summary>
public sealed record BondReport(BondPlan Plan, ImmutableArray<ExitOutcome> Exits);

/// <summary>
/// Everything that a writer needs. It holds the inputs as well as the results,
/// because a result without its scenario cannot be read.
/// </summary>
public sealed record CalculationReport(
    Scenario Scenario,
    DateOnly Purchase,
    int Units,
    decimal TaxRate,
    ImmutableArray<BondReport> Bonds);

/// <summary>The figure that the comparison matrix shows in each cell.</summary>
public enum ExitMetric
{
    /// <summary>Net profit on the whole holding, in zloty.</summary>
    NetProfit,

    /// <summary>Net profit as a part of the money paid.</summary>
    NetProfitRate,

    /// <summary>Yearly net internal rate of return.</summary>
    AnnualisedNetReturn,
}

/// <summary>Turns a report into text.</summary>
public interface IReportWriter
{
    /// <summary>The name that the command line uses to pick this writer.</summary>
    string Format { get; }

    string Render(CalculationReport report);
}
