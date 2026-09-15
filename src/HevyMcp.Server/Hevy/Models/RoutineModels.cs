namespace HevyMcp.Server.Hevy.Models;

public record RoutinesPage(
    int Page,
    int PageCount,
    IReadOnlyList<Routine> Routines
);

public record Routine(
    string Id,
    string Title,
    IReadOnlyList<RoutineExercise> Exercises
);

public record RoutineExercise(
    string Title,
    string ExerciseTemplateId
);
