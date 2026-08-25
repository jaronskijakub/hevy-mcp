using HevyMcp.Server.Hevy.Models;
using HevyMcp.Server.Tools.Models;

namespace HevyMcp.Server.Hevy.Services;

public class PerformedExercises(HevyClient hevy)
{
    private const int PageSize = 10;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private List<ExerciseSummary>? _summaries;

    /// <summary>Exercises the user has actually performed, most frequent first.
    /// When a query is given, only names containing it are returned.</summary>
    public async Task<IReadOnlyList<ExerciseSummary>> FindAsync(string? query = null)
    {
        await _gate.WaitAsync();
        try
        {
            _summaries ??= Summarize(await FetchAllAsync());

            if (query is not { Length: > 0 }) return _summaries;

            return _summaries
                .Where(summary => summary.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<Workout>> FetchAllAsync()
    {
        var all = new List<Workout>();
        var page = 1;

        while (true)
        {
            var response = await hevy.GetWorkoutPageAsync(page, PageSize);
            all.AddRange(response.Workouts);

            if (page >= response.PageCount) break;

            page++;
        }

        return all;
    }

    private static List<ExerciseSummary> Summarize(List<Workout> workouts) =>
        workouts
            .SelectMany(workout =>
                workout.Exercises.Select(exercise => new { exercise.Title, WorkoutId = workout.Id, workout.StartTime }))
            .GroupBy(entry => entry.Title, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ExerciseSummary(
                group.First().Title,
                group.DistinctBy(entry => entry.WorkoutId).Count(),
                group.Max(entry => entry.StartTime)))
            .OrderByDescending(summary => summary.Sessions)
            .ToList();
}