using HevyMcp.Server.Hevy.Models;

namespace HevyMcp.Server.Hevy.Services;

/// <summary>Every exercise Hevy knows about, keyed by title. The catalog does not change
/// while the server runs, so it is fetched once and kept in memory.</summary>
public class ExerciseCatalog(HevyClient hevy)
{
    private const int PageSize = 100;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private Dictionary<string, ExerciseTemplate>? _map;

    /// <summary>Resolves an exercise name to its Hevy template id, or null if unknown.</summary>
    public async Task<string?> FindExerciseTemplateIdAsync(string exerciseName) =>
        (await GetMapAsync()).GetValueOrDefault(exerciseName)?.Id;

    /// <summary>The full template for an exercise name, or null if unknown.</summary>
    public async Task<ExerciseTemplate?> FindAsync(string exerciseName) =>
        (await GetMapAsync()).GetValueOrDefault(exerciseName);

    /// <summary>Every exercise template Hevy knows about.</summary>
    public async Task<IReadOnlyCollection<ExerciseTemplate>> AllAsync() =>
        (await GetMapAsync()).Values;

    private async Task<Dictionary<string, ExerciseTemplate>> GetMapAsync()
    {
        await _gate.WaitAsync();
        try
        {
            return _map ??= BuildMap(await FetchAllAsync());
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<ExerciseTemplate>> FetchAllAsync()
    {
        var all = new List<ExerciseTemplate>();
        var page = 1;

        while (true)
        {
            var response = await hevy.GetExerciseTemplatePageAsync(page, PageSize);
            all.AddRange(response.ExerciseTemplates);

            if (page >= response.PageCount) break;

            page++;
        }

        return all;
    }

    private static Dictionary<string, ExerciseTemplate> BuildMap(List<ExerciseTemplate> templates) =>
        templates
            .DistinctBy(template => template.Title, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(template => template.Title, StringComparer.OrdinalIgnoreCase);
}
