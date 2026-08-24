namespace HevyMcp.Server.Hevy.Models;

public record ExerciseTemplatesPage(int Page, int PageCount, IReadOnlyList<ExerciseTemplate> ExerciseTemplates);

public record ExerciseTemplate(string Id, string Title, string Type, string Equipment, bool IsCustom);