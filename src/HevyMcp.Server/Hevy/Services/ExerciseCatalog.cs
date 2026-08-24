using HevyMcp.Server.Hevy.Models;
namespace HevyMcp.Server.Hevy.Services;

public class ExerciseCatalog(HevyClient hevy)
{
    private const int PageSize = 100;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private Dictionary<string, string>? _map;

    /// <summary>Resolves an exercise name to its Hevy template id, or null if unknown.</summary>
    public async Task<string?> FindExerciseTemplateIdAsync(string exerciseName)
    {
        await _gate.WaitAsync();
        try
        {
            _map ??= BuildMap(await FetchAllAsync());

            return _map.GetValueOrDefault(exerciseName);
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

    private static Dictionary<string, string> BuildMap(List<ExerciseTemplate> templates)
    {
        return templates
            .DistinctBy(template => template.Title, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(template => template.Title, template => template.Id, StringComparer.OrdinalIgnoreCase);
    }
}