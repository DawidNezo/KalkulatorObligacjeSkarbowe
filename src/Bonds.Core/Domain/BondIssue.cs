namespace Bonds.Core.Domain;

/// <summary>
/// The parameters of one bond issue, taken from its issue letter. The engine holds
/// the mechanism and this record holds every number, thus a new issue needs data
/// only and no code.
/// </summary>
public sealed record BondIssue(
    string Code,
    BondKind Kind,
    Money Nominal,
    Money PurchasePrice,
    int PeriodsPerYear,
    int PeriodCount,
    bool Capitalizes,
    bool RoundCapitalizedBase,
    PrincipalFloor Floor,
    RateRule Rate,
    FeeRule Fee,
    EarlyRedemptionWindow EarlyRedemption,
    DateOnly SaleStart,
    DateOnly SaleEnd,
    string Source)
{
    public const int MonthsPerYear = 12;

    /// <summary>
    /// Months in one interest period. Three for OTS, one for ROR and DOR, twelve for
    /// the rest. The value is computed and not stored, because a "with" expression
    /// does not run the constructor body and a stored value would go stale.
    /// </summary>
    public int PeriodMonths => MonthsPerYear / EnsurePeriodsPerYearDividesTheYear();

    /// <summary>The whole term of the bond, in months.</summary>
    public int TermMonths => PeriodCount * PeriodMonths;

    /// <summary>True when the bond pays the interest of every period out instead of holding it.</summary>
    public bool PaysCoupons => !Capitalizes;

    /// <summary>Validates the parts that no type can express.</summary>
    public BondIssue Validated()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Code);
        ArgumentException.ThrowIfNullOrWhiteSpace(Source);
        ArgumentOutOfRangeException.ThrowIfLessThan(PeriodCount, 1);
        EnsurePeriodsPerYearDividesTheYear();
        ArgumentNullException.ThrowIfNull(Rate);
        ArgumentNullException.ThrowIfNull(Fee);
        ArgumentNullException.ThrowIfNull(EarlyRedemption);

        if (Nominal <= Money.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(Nominal), "Nominał musi być większy od zera.");
        }

        if (PurchasePrice <= Money.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(PurchasePrice), "Cena zakupu musi być większa od zera.");
        }

        // A price below the face value means a discount (zamiana). The discount is
        // taxable at redemption (art. 30a ust. 1 pkt 2 PIT) and this calculator
        // does not model that tax, thus it refuses the input instead of showing a
        // net profit that is too high.
        if (PurchasePrice != Nominal)
        {
            throw new ArgumentException(
                $"Emisja {Code}: cena zakupu {PurchasePrice} zł różni się od nominału {Nominal} zł. "
                + "Kalkulator modeluje wyłącznie zakup za gotówkę po cenie równej nominałowi, "
                + "bo nie liczy podatku od dyskonta (np. przy zamianie).",
                nameof(PurchasePrice));
        }

        if (SaleEnd < SaleStart)
        {
            throw new ArgumentException($"Emisja {Code}: sprzedaż kończy się przed jej początkiem.", nameof(SaleEnd));
        }

        if (RoundCapitalizedBase && !Capitalizes)
        {
            throw new ArgumentException(
                $"Emisja {Code} nie kapitalizuje odsetek, więc nie ma bazy do zaokrąglania.",
                nameof(RoundCapitalizedBase));
        }

        return this;
    }

    /// <summary>Throws when the purchase date is outside the sale window of the issue.</summary>
    public void EnsureCanBuyOn(DateOnly purchase)
    {
        if (purchase < SaleStart || purchase > SaleEnd)
        {
            throw new ArgumentOutOfRangeException(nameof(purchase),
                $"Emisja {Code} jest w sprzedaży od {SaleStart:yyyy-MM-dd} do {SaleEnd:yyyy-MM-dd}, "
                + $"więc nie można jej kupić {purchase:yyyy-MM-dd}.");
        }
    }

    private int EnsurePeriodsPerYearDividesTheYear()
    {
        if (PeriodsPerYear < 1 || MonthsPerYear % PeriodsPerYear != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(PeriodsPerYear),
                $"{PeriodsPerYear} okresów odsetkowych w roku nie dzieli {MonthsPerYear} miesięcy.");
        }

        return PeriodsPerYear;
    }
}
