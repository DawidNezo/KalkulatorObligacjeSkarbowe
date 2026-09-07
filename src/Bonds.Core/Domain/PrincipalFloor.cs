namespace Bonds.Core.Domain;

/// <summary>
/// The scope of the rule that an early redemption never pays less than the face
/// value. The letters give this rule a different scope for different bonds.
/// </summary>
public enum PrincipalFloor
{
    /// <summary>No floor. Used by OTS, whose early redemption always pays the face value.</summary>
    None,

    /// <summary>The floor applies only in the first interest period. ROR, DOR and COI.</summary>
    FirstPeriodOnly,

    /// <summary>The floor applies in every interest period. TOS, EDO, ROS and ROD.</summary>
    AllPeriods,
}
