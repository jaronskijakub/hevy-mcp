using System.ComponentModel;
using HevyMcp.Server.Analysis;
using HevyMcp.Server.Hevy.Models;
using HevyMcp.Server.Hevy.Services;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HevyMcp.Server.Tools;

[McpServerToolType]
public class ProgressTool(HevyClient hevyClient, ExerciseCatalog catalogClient)
{
    [McpServerTool(Name = "get_exercise_progress")]
    [Description(
        "Computes a strength trend for one exercise from the user's Hevy training history. "
        + "Returns the slope of estimated 1RM over time in kg per month, fitted by linear regression "
        + "across every session containing that exercise, plus the first and last estimated 1RM "
        + "and how many sessions went into the fit. "
        + "Per session, estimated 1RM is the best working set by the Epley formula; warmup sets and "
        + "sets with no recorded weight are excluded. "
        + "Use this for questions about progress, plateaus or stalling - \"am I getting stronger at X\", "
        + "\"have I stalled on Y\". "
        + "\"am I progressing on Z (Weighted)\". "
        + "Prefer it over reading raw workouts: the arithmetic is done here, "
        + "over the full history, not estimated from a sample. "
        + "Note that for bodyweight exercises Hevy records only the added weight, so the numbers are "
        + "relative to the user's bodyweight, not absolute loads. "
        + "Fails when the exercise name is not in the user's Hevy catalog, or when fewer than two "
        + "sessions have a recorded weight.")]
    public async Task<ExerciseProgress> GetExerciseProgress([Description(
            "Exercise title exactly as it appears in Hevy, in English - for example \"Dumbbell Row\" "
            + "or \"Bench Press (Barbell)\". Letter case is ignored, nothing else is: plurals, "
            + "abbreviations or reworded names will not match.")]
        string exerciseName)
    {
        var exerciseTemplateId = await catalogClient.FindExerciseTemplateIdAsync(exerciseName);

        if (exerciseTemplateId is null)
            throw new McpException($"Unknown exercise: '{exerciseName}'.");

        var history = await hevyClient.GetExerciseHistoryAsync(exerciseTemplateId);
        var sessions = history.ExerciseHistory
            .GroupBy(entry => entry.WorkoutId)
            .ToList();

        var points = sessions
            .Select(ToPoint)
            .OfType<E1RmPoint>()
            .OrderBy(point => point.Date)
            .ToList();

        var trend = Trend.KgPerMonth(points);

        if (trend is not {} kgPerMonth)
            throw new McpException($"Not enough data to compute a trend for '{exerciseName}'.");

        return new ExerciseProgress(
            exerciseName,
            points.Count,
            points[0].Date,
            points[^1].Date,
            Math.Round(points[0].E1Rm, 1),
            Math.Round(points[^1].E1Rm, 1),
            Math.Round(kgPerMonth, 2));
    }

    private static E1RmPoint? ToPoint(IGrouping<string, ExerciseHistoryEntry> session)
    {
        var sets = session.Select(entry => new CompletedSet(entry.SetType, entry.WeightKg, entry.Reps));

        return OneRepMax.BestOf(sets) is {} best
            ? new E1RmPoint(session.First().WorkoutStartTime, best)
            : null;
    }
}