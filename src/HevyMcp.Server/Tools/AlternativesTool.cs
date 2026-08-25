using System.ComponentModel;
using HevyMcp.Server.Analysis;
using HevyMcp.Server.Hevy.Models;
using HevyMcp.Server.Hevy.Services;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HevyMcp.Server.Tools;

[McpServerToolType]
public class AlternativesTool(ExerciseCatalog catalog, PerformedExercises performed)
{
    [McpServerTool(Name = "get_exercise_alternatives")]
    [Description(
        "Finds substitutes for an exercise when its equipment is unavailable - a machine is taken, "
        + "a rack is busy, the gym does not have it. "
        + "Returns exercises that work the same muscle group with DIFFERENT equipment, ordered so "
        + "the ones the user already trains come first; sessions says how many times each has been "
        + "logged, and 0 means the user has never done it. "
        + "The match field says how the list was found: Primary means same primary muscle group, "
        + "Secondary means the muscle is only worked indirectly and the substitute is weaker, "
        + "None means nothing suitable exists - say so rather than inventing a replacement. "
        + "This searches the user's Hevy catalog, not the equipment their gym actually has, so "
        + "present the results as options to pick from.")]
    public async Task<AlternativeMatch> GetExerciseAlternatives(
        [Description(
            "Exercise title exactly as it appears in Hevy - for example \"Chest Fly (Machine)\". "
            + "Use get_exercise_catalog first when the exact title is not known.")]
        string exerciseName)
    {
        var target = await catalog.FindAsync(exerciseName);

        if (target is null)
            throw new McpException($"Unknown exercise: '{exerciseName}'.");

        var sessions = (await performed.FindAsync())
            .ToDictionary(summary => summary.Name, summary => summary.Sessions, StringComparer.OrdinalIgnoreCase);

        // TODO: exclude exercises already scheduled in the routine the user is doing today
        // (GET /v1/routines) - suggesting something they are about to perform anyway is useless.
        var allExercises = (await catalog.AllAsync())
            .Select(template => ToExerciseProfile(template, sessions));

        var targetProfile = ToExerciseProfile(target, sessions);

        return Alternatives.For(targetProfile, allExercises);
    }

    private static ExerciseProfile ToExerciseProfile(ExerciseTemplate template, Dictionary<string, int> sessions) =>
        new(template.Title,
            template.PrimaryMuscleGroup,
            template.Equipment,
            template.Type,
            sessions.GetValueOrDefault(template.Title),
            template.SecondaryMuscleGroups);
}