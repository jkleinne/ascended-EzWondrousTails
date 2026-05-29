using System;

namespace WondrousTailsSolver;

/// <summary>
/// Pure helpers for the <see cref="ShuffleObjective"/> setting, kept out of the
/// Dalamud-bound config type so the default and validation are unit-testable.
/// </summary>
public static class ShuffleObjectives {
    /// <summary>Default advice objective: maximize the two-line chance.</summary>
    public const ShuffleObjective Default = ShuffleObjective.TwoLineMax;

    /// <summary>Clamps a deserialized value to a defined member, falling back to <see cref="Default"/>.</summary>
    public static ShuffleObjective Normalize(ShuffleObjective value)
        => Enum.IsDefined(value) ? value : Default;
}
