using Bonds.Core.Domain;

namespace Bonds.Core.Tests.Support;

/// <summary>
/// The eight issues of 21 July 2026, built in code. The engine tests use these so
/// that a change in a data file cannot hide a change in the mechanism.
/// </summary>
internal static class TestIssues
{
    internal static readonly DateOnly Purchase = new(2026, 8, 17);

    private static readonly DateOnly SaleStart = new(2026, 8, 1);
    private static readonly DateOnly SaleEnd = new(2026, 8, 31);
    private static readonly EarlyRedemptionWindow Window = new(7, 20);
    private static readonly Money Nominal = Money.RoundToGrosz(100m);

    internal static BondIssue Ots() => Build(
        "OTS1126", BondKind.Ots, periodsPerYear: 4, periodCount: 1,
        capitalizes: false, roundCapitalizedBase: false, PrincipalFloor.None,
        new FixedRate(0.0200m), new ForfeitAllInterestFee());

    internal static BondIssue Ror() => Build(
        "ROR0827", BondKind.Ror, periodsPerYear: 12, periodCount: 12,
        capitalizes: false, roundCapitalizedBase: false, PrincipalFloor.FirstPeriodOnly,
        new NbpReferenceLinkedRate(0.0400m, 0.0000m), new TwoTierFee(Money.RoundToGrosz(0.50m)));

    internal static BondIssue Dor() => Build(
        "DOR0828", BondKind.Dor, periodsPerYear: 12, periodCount: 24,
        capitalizes: false, roundCapitalizedBase: false, PrincipalFloor.FirstPeriodOnly,
        new NbpReferenceLinkedRate(0.0415m, 0.0015m), new TwoTierFee(Money.RoundToGrosz(0.70m)));

    internal static BondIssue Tos() => Build(
        "TOS0829", BondKind.Tos, periodsPerYear: 1, periodCount: 3,
        capitalizes: true, roundCapitalizedBase: true, PrincipalFloor.AllPeriods,
        new FixedRate(0.0440m), new AccruedCappedFee(Money.RoundToGrosz(1.00m)));

    internal static BondIssue Coi() => Build(
        "COI0830", BondKind.Coi, periodsPerYear: 1, periodCount: 4,
        capitalizes: false, roundCapitalizedBase: false, PrincipalFloor.FirstPeriodOnly,
        new InflationLinkedRate(0.0475m, 0.0150m), new TwoTierFee(Money.RoundToGrosz(2.00m)));

    internal static BondIssue Ros() => Build(
        "ROS0832", BondKind.Ros, periodsPerYear: 1, periodCount: 6,
        capitalizes: true, roundCapitalizedBase: false, PrincipalFloor.AllPeriods,
        new InflationLinkedRate(0.0500m, 0.0200m), new AccruedCappedFee(Money.RoundToGrosz(2.00m)));

    internal static BondIssue Edo() => Build(
        "EDO0836", BondKind.Edo, periodsPerYear: 1, periodCount: 10,
        capitalizes: true, roundCapitalizedBase: false, PrincipalFloor.AllPeriods,
        new InflationLinkedRate(0.0535m, 0.0200m), new AccruedCappedFee(Money.RoundToGrosz(3.00m)));

    internal static BondIssue Rod() => Build(
        "ROD0838", BondKind.Rod, periodsPerYear: 1, periodCount: 12,
        capitalizes: true, roundCapitalizedBase: false, PrincipalFloor.AllPeriods,
        new InflationLinkedRate(0.0560m, 0.0250m), new AccruedCappedFee(Money.RoundToGrosz(3.00m)));

    internal static IEnumerable<BondIssue> All()
    {
        yield return Ots();
        yield return Ror();
        yield return Dor();
        yield return Tos();
        yield return Coi();
        yield return Ros();
        yield return Edo();
        yield return Rod();
    }

    private static BondIssue Build(
        string code,
        BondKind kind,
        int periodsPerYear,
        int periodCount,
        bool capitalizes,
        bool roundCapitalizedBase,
        PrincipalFloor floor,
        RateRule rate,
        FeeRule fee) =>
        new BondIssue(
            code, kind, Nominal, Nominal, periodsPerYear, periodCount, capitalizes,
            roundCapitalizedBase, floor, rate, fee, Window, SaleStart, SaleEnd,
            $"test fixture for {code}").Validated();
}
