namespace HevyMcp.Server.Analysis;

/// <summary>An exercise reduced to what matters when looking for a substitute.</summary>
public record ExerciseProfile(
    string Name,
    string MuscleGroup,
    string Equipment,
    string Type,
    int Sessions,
    IReadOnlyList<string> SecondaryMuscleGroups
);