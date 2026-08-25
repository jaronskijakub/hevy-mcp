namespace HevyMcp.Server.Tools.Models;

/// <summary>One exercise the user has actually performed.</summary>
public record ExerciseSummary(string Name, int Sessions, DateTimeOffset LastPerformed);