namespace Bonds.Core.Domain;

/// <summary>The short name of a retail treasury bond family.</summary>
public enum BondKind
{
    /// <summary>Three-month, fixed rate.</summary>
    Ots,

    /// <summary>One-year, tied to the NBP reference rate.</summary>
    Ror,

    /// <summary>Two-year, tied to the NBP reference rate.</summary>
    Dor,

    /// <summary>Three-year, fixed rate, yearly capitalisation.</summary>
    Tos,

    /// <summary>Four-year, tied to the CPI.</summary>
    Coi,

    /// <summary>Ten-year, tied to the CPI, yearly capitalisation.</summary>
    Edo,

    /// <summary>Six-year family bond, tied to the CPI, yearly capitalisation.</summary>
    Ros,

    /// <summary>Twelve-year family bond, tied to the CPI, yearly capitalisation.</summary>
    Rod,
}
