namespace Bonds.Core.Engine;

/// <summary>What happens when the holder leaves the bond in a given month.</summary>
public enum ExitKind
{
    /// <summary>The bond is called back before the end of its term, thus a fee can apply.</summary>
    EarlyRedemption,

    /// <summary>The term ends. No fee applies.</summary>
    Maturity,

    /// <summary>The issue letter does not allow a call for an early redemption on this date.</summary>
    NotAvailable,

    /// <summary>The month is beyond the term, thus the bond no longer exists.</summary>
    DoesNotExist,
}
