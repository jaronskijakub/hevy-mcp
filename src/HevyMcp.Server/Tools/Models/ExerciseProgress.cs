public record ExerciseProgress(
    string Exercise,
    int Sessions,
    DateTimeOffset From,
    DateTimeOffset To,
    double FirstE1Rm,
    double LastE1Rm,
    double KgPerMonth);