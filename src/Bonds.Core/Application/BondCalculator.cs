using System.Collections.Immutable;
using Bonds.Core.Calendar;
using Bonds.Core.Data;
using Bonds.Core.Domain;
using Bonds.Core.Engine;
using Bonds.Core.Market;
using Bonds.Core.Reporting;

namespace Bonds.Core.Application;

/// <summary>
/// Puts the parts together: read the data, build a plan for each issue and
/// evaluate every month of exit. This is the only class that the command line
/// needs.
/// </summary>
public sealed class BondCalculator
{
    private readonly JsonDataStore store;
    private readonly IBusinessDayCalendar calendar;

    public BondCalculator(JsonDataStore store, IBusinessDayCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(calendar);

        this.store = store;
        this.calendar = calendar;
    }

    public CalculationReport Run(CalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validated();

        var scenario = store.LoadScenario(request.ScenarioPath);
        var market = new MarketData(
            store.LoadDatedSeries(request.NbpSeriesPath),
            store.LoadMonthlySeries(request.CpiSeriesPath),
            calendar,
            scenario.AssumedNbpReference,
            scenario.AssumedCpi);

        var directory = MonthlyIssueDirectory.For(request.IssuesDirectory, request.Purchase);
        var issues = Select(store.LoadIssues(directory), request.OnlyCodes);
        var evaluator = new ExitEvaluator(new RedemptionCalculator(), new TaxCalculator(request.TaxRate));

        var reports = ImmutableArray.CreateBuilder<BondReport>(issues.Count);
        foreach (var issue in issues)
        {
            var plan = BondPlan.Create(issue, request.Purchase, market);
            reports.Add(new BondReport(plan, evaluator.Evaluate(plan, request.Units)));
        }

        return new CalculationReport(
            scenario, request.Purchase, request.Units, request.TaxRate, reports.MoveToImmutable());
    }

    private static IReadOnlyList<BondIssue> Select(
        IReadOnlyList<BondIssue> issues, IReadOnlyCollection<string> onlyCodes)
    {
        if (onlyCodes.Count == 0)
        {
            return issues;
        }

        var wanted = new HashSet<string>(onlyCodes, StringComparer.OrdinalIgnoreCase);
        var selected = issues.Where(i => wanted.Contains(i.Code)).ToList();

        var missing = wanted.Except(selected.Select(i => i.Code), StringComparer.OrdinalIgnoreCase).ToList();
        if (missing.Count > 0)
        {
            throw new ArgumentException(
                $"Żaden plik emisji nie pasuje do: {string.Join(", ", missing.Order(StringComparer.Ordinal))}.");
        }

        return selected;
    }
}
