namespace HevyMcp.Server.Hevy.Models;

public record ExerciseHistoryResponse(IReadOnlyList<ExerciseHistoryEntry> ExerciseHistory);

public record ExerciseHistoryEntry(
    string WorkoutId,
    DateTimeOffset WorkoutStartTime,
    double? WeightKg,
    int? Reps,
    string SetType);