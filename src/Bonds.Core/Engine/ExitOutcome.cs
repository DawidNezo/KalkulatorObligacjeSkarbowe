namespace Bonds.Core.Engine;

/// <summary>
/// One row of the exit table: what the holder gets if they leave in month
/// <paramref name="MonthIndex"/>. <see cref="Details"/> is null when no exit can
/// happen in that month.
/// </summary>
public sealed record ExitOutcome(int MonthIndex, DateOnly ExitDate, ExitKind Kind, ExitDetails? Details);
