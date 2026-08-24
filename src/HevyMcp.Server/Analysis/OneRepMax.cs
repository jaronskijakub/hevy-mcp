namespace HevyMcp.Server.Analysis;

public static class OneRepMax
{
    private const double EpleyDivisor = 30.0;

    /// <summary>Epley formula: weight × (1 + reps / 30).</summary>
    public static double Epley(double weightKg, int reps) =>
        weightKg * (1 + reps / EpleyDivisor);

    private static double? ForSet(CompletedSet set) =>
        set is { Type: "normal", WeightKg: {} weightKg, Reps: {} reps }
            ? Epley(weightKg, reps)
            : null;

    /// <summary>Best estimated 1RM across the sets of one exercise,
    /// or null if no set counts.</summary>
    public static double? BestOf(IEnumerable<CompletedSet> sets) =>
        sets.Select(ForSet).Max();
}