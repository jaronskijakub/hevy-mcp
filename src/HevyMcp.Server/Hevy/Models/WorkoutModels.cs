namespace HevyMcp.Server.Hevy.Models;

public record WorkoutsPage(
    int Page,
    int PageCount,
    IReadOnlyList<Workout> Workouts
);

public record Workout(
    string Id,
    string Title,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    IReadOnlyList<WorkoutExercise> Exercises
);

public record WorkoutExercise(
    int Index,
    string Title,
    string ExerciseTemplateId,
    IReadOnlyList<WorkoutSet> Sets);

public record WorkoutSet(
    int Index,
    string Type,
    double? WeightKg,
    int? Reps,
    double? Rpe);