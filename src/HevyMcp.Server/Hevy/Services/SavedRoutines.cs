using HevyMcp.Server.Hevy.Models;

namespace HevyMcp.Server.Hevy.Services;

public class SavedRoutines(HevyClient hevy)
{
    private const int PageSize = 10;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private IReadOnlyList<Routine>? _routines;

    public async Task<IReadOnlyList<Routine>> AllAsync()
    {
        await _gate.WaitAsync();
        try
        {
            return _routines ??= await FetchAllAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<Routine>> FindByExerciseAsync(string exerciseTemplateId)
    {
        var routines = await AllAsync();

        return routines
            .Where(routine => routine.Exercises.Any(
                exercise => exercise.ExerciseTemplateId == exerciseTemplateId))
            .ToList();
    }

    private async Task<IReadOnlyList<Routine>> FetchAllAsync()
    {
        var all = new List<Routine>();
        var page = 1;

        while (true)
        {
            var response = await hevy.GetRoutinePageAsync(page, PageSize);
            all.AddRange(response.Routines);

            if (page >= response.PageCount) break;

            page++;
        }

        return all;
    }
}
