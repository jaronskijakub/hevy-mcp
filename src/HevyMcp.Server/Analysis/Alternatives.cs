namespace HevyMcp.Server.Analysis;

/// <summary>How well the alternatives match the exercise being replaced.</summary>
public enum MatchQuality
{
    Primary,
    Secondary,
    None
}

/// <summary>Substitutes for an exercise, together with how well they match.</summary>
public record AlternativeMatch(MatchQuality Match, IReadOnlyList<ExerciseProfile> Exercises);

/// <summary>Picks substitutes for an exercise whose equipment is unavailable.</summary>
public static class Alternatives
{
    public static AlternativeMatch For(ExerciseProfile target, IEnumerable<ExerciseProfile> candidates)
    {
        var pool = candidates
            .Where(candidate => candidate.Equipment != target.Equipment
                                && candidate.Type == target.Type)
            .ToList();

        var primary = Ranked(pool.Where(candidate => candidate.MuscleGroup == target.MuscleGroup));

        if (primary.Count > 0) return new AlternativeMatch(MatchQuality.Primary, primary);

        var secondary = Ranked(pool.Where(candidate => candidate.SecondaryMuscleGroups.Contains(target.MuscleGroup)));

        return secondary.Count > 0
            ? new AlternativeMatch(MatchQuality.Secondary, secondary)
            : new AlternativeMatch(MatchQuality.None, []);
    }

    /// <summary>Exercises the user trains most often come first.</summary>
    private static List<ExerciseProfile> Ranked(IEnumerable<ExerciseProfile> matches) =>
        matches
            .OrderByDescending(candidate => candidate.Sessions)
            .ThenBy(candidate => candidate.Name)
            .ToList();
}
