namespace Bonds.Core.Application;

/// <summary>Everything that one run of the calculator needs.</summary>
public sealed record CalculationRequest(
    string IssuesDirectory,
    string ScenarioPath,
    string NbpSeriesPath,
    string CpiSeriesPath,
    DateOnly Purchase,
    int Units,
    decimal TaxRate,
    IReadOnlyCollection<string> OnlyCodes)
{
    public CalculationRequest Validated()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(IssuesDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(ScenarioPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(NbpSeriesPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(CpiSeriesPath);
        ArgumentOutOfRangeException.ThrowIfLessThan(Units, 1);
        ArgumentNullException.ThrowIfNull(OnlyCodes);

        if (TaxRate < 0m || TaxRate > 1m)
        {
            // The user types the rate in percent (--tax 19), thus the message
            // speaks percent even though the field holds a fraction.
            throw new ArgumentOutOfRangeException(nameof(TaxRate), "Stawka podatku musi być między 0% a 100%.");
        }

        return this;
    }
}
