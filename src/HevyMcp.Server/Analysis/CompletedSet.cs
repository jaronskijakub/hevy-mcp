namespace HevyMcp.Server.Analysis;

/// <summary>One completed set, reduced to the fields the calculators need.</summary>
public record CompletedSet(string Type, double? WeightKg, int? Reps);