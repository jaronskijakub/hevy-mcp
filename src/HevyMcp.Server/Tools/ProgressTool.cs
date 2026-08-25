using System.ComponentModel;
using HevyMcp.Server.Analysis;
using HevyMcp.Server.Hevy.Models;
using HevyMcp.Server.Hevy.Services;
using HevyMcp.Server.Tools.Models;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HevyMcp.Server.Tools;

[McpServerToolType]
public class ProgressTool(HevyClient hevyClient, ExerciseCatalog catalogClient)
{
    private const int RecentWindowMonths = 3;

    [McpServerTool(Name = "get_exercise_progress")]
    [Description(
        "Computes a strength trend for one exercise from the user's Hevy training history. "
        + "Returns the slope of estimated 1RM over time in kg per month, fitted by linear regression "
        + "across every session containing that exercise, plus the first and last estimated 1RM "
        + "and how many sessions went into the fit. "
        + "Two slopes come back: kgPerMonth is fitted over the whole recorded history, while "
        + "recentKgPerMonth covers only the last three months, with recentSessions saying how many "
        + "sessions fall in that window. Compare them - a clearly positive kgPerMonth together with "
        + "a near-zero or negative recentKgPerMonth means the lift has stalled lately even though "
        + "the long-term trend is up. recentKgPerMonth is null when the window holds fewer than "
        + "two sessions, which is not an error. "
        + "These two windows are the only periods measured. Do not describe how the trend behaved "
        + "at any other point in time - no such breakdown is computed, and asserting one would be "
        + "a guess. "
        + "Per session, estimated 1RM is the best working set by the Epley formula; warmup sets and "
        + "sets with no recorded weight are excluded. "
        + "Use this for questions about progress, plateaus or stalling - \"am I getting stronger at X\", "
        + "\"have I stalled on Y\", "
        + "\"am I progressing on Z (Weighted)\". "
        + "Prefer it over reading raw workouts: the arithmetic is done here, "
        + "over the full history, not estimated from a sample. "
        + "Note that for bodyweight exercises Hevy records only the added weight, so the numbers are "
        + "relative to the user's bodyweight, not absolute loads. "
        + "Fails when the exercise name is not in the user's Hevy catalog, or when fewer than two "
        + "sessions have a recorded weight. "
        + "If the exact Hevy title is not known, or this call fails with an unknown exercise, "
        + "call get_exercise_catalog first to find the real title, then retry.")]
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

        var cutoff = points[^1].Date.AddMonths(-RecentWindowMonths);
        var recent = points.Where(point => point.Date >= cutoff).ToList();
        var recentKgPerMonth = Trend.KgPerMonth(recent);

        return new ExerciseProgress(
            exerciseName,
            points.Count,
            points[0].Date,
            points[^1].Date,
            Math.Round(points[0].E1Rm, 1),
            Math.Round(points[^1].E1Rm, 1),
            Math.Round(kgPerMonth, 2),
            recent.Count,
            recentKgPerMonth is {} value ? Math.Round(value, 2) : null);
    }

    private static E1RmPoint? ToPoint(IGrouping<string, ExerciseHistoryEntry> session)
    {
        var sets = session.Select(entry => new CompletedSet(entry.SetType, entry.WeightKg, entry.Reps));

        return OneRepMax.BestOf(sets) is {} best
            ? new E1RmPoint(session.First().WorkoutStartTime, best)
            : null;
    }
}