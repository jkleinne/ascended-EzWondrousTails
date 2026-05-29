using Dalamud.Configuration;

namespace WondrousTailsSolver;

/// <summary>
/// Persisted display preferences that Dalamud serializes between plugin sessions.
/// </summary>
public sealed class PluginConfiguration : IPluginConfiguration {
    private const int CurrentVersion = 1;
    private const int WholePercentDecimalPlaces = 0;
    private const int OneDecimalPlace = 1;
    private const int TwoDecimalPlaces = 2;

    /// <summary>
    /// Stores the config schema version so future releases can normalize older files.
    /// </summary>
    public int Version { get; set; } = CurrentVersion;

    /// <summary>
    /// Controls whether the plugin appends probability output to the Wondrous Tails journal.
    /// </summary>
    public bool EnableJournalOverlay { get; set; } = true;

    /// <summary>
    /// Controls whether one, two, and three line chances are included in display output.
    /// </summary>
    public bool ShowLineChances { get; set; } = true;

    /// <summary>
    /// Controls whether shuffle baseline averages are included when shuffling is available.
    /// </summary>
    public bool ShowShuffleAverage { get; set; } = true;

    /// <summary>
    /// Controls whether the keep, neutral, or shuffle recommendation is included.
    /// </summary>
    public bool ShowShuffleAdvice { get; set; } = true;

    /// <summary>
    /// Controls whether Dalamud foreground and glow colors are applied to probability output.
    /// </summary>
    public bool UseColoredJournalText { get; set; } = true;

    /// <summary>
    /// Controls the number of decimal places shown for percentage values.
    /// </summary>
    public ProbabilityPrecision DecimalPlaces { get; set; } = ProbabilityPrecision.TwoDecimalPlaces;

    /// <summary>
    /// Which line objective the shuffle advice optimizes its keep/shuffle verdict on.
    /// </summary>
    public ShuffleObjective Objective { get; set; } = ShuffleObjectives.Default;

    internal bool HasAnyDisplaySectionEnabled
        => ShowLineChances || ShowShuffleAverage || ShowShuffleAdvice;

    internal int ProbabilityDecimalPlaces
        => DecimalPlaces switch {
            ProbabilityPrecision.WholePercent => WholePercentDecimalPlaces,
            ProbabilityPrecision.OneDecimalPlace => OneDecimalPlace,
            ProbabilityPrecision.TwoDecimalPlaces => TwoDecimalPlaces,
            _ => TwoDecimalPlaces,
        };

    internal void ResetToDefaults() {
        Version = CurrentVersion;
        EnableJournalOverlay = true;
        ShowLineChances = true;
        ShowShuffleAverage = true;
        ShowShuffleAdvice = true;
        UseColoredJournalText = true;
        DecimalPlaces = ProbabilityPrecision.TwoDecimalPlaces;
        Objective = ShuffleObjectives.Default;
    }

    internal bool Normalize() {
        var originalVersion = Version;
        var originalDecimalPlaces = DecimalPlaces;
        var originalObjective = Objective;

        Version = CurrentVersion;
        DecimalPlaces = DecimalPlaces switch {
            ProbabilityPrecision.WholePercent => ProbabilityPrecision.WholePercent,
            ProbabilityPrecision.OneDecimalPlace => ProbabilityPrecision.OneDecimalPlace,
            ProbabilityPrecision.TwoDecimalPlaces => ProbabilityPrecision.TwoDecimalPlaces,
            _ => ProbabilityPrecision.TwoDecimalPlaces,
        };
        Objective = ShuffleObjectives.Normalize(Objective);

        return Version != originalVersion
            || DecimalPlaces != originalDecimalPlaces
            || Objective != originalObjective;
    }

    internal static ProbabilityPrecision FromDecimalPlaces(int decimalPlaces)
        => decimalPlaces switch {
            WholePercentDecimalPlaces => ProbabilityPrecision.WholePercent,
            OneDecimalPlace => ProbabilityPrecision.OneDecimalPlace,
            TwoDecimalPlaces => ProbabilityPrecision.TwoDecimalPlaces,
            _ => ProbabilityPrecision.TwoDecimalPlaces,
        };

    internal static int ToObjectiveIndex(ShuffleObjective objective)
        => (int)objective;

    internal static ShuffleObjective FromObjectiveIndex(int index)
        => ShuffleObjectives.Normalize((ShuffleObjective)index);
}

/// <summary>
/// Supported precision levels for displayed Wondrous Tails percentages.
/// </summary>
public enum ProbabilityPrecision {
    /// <summary>
    /// Shows percentages rounded to whole numbers for compact display.
    /// </summary>
    WholePercent = 0,

    /// <summary>
    /// Shows one decimal place for a balance of compactness and detail.
    /// </summary>
    OneDecimalPlace = 1,

    /// <summary>
    /// Shows two decimal places to match the plugin's original output precision.
    /// </summary>
    TwoDecimalPlaces = 2,
}
