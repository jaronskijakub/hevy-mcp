using System.ComponentModel;
using HevyMcp.Server.Analysis;
using HevyMcp.Server.Hevy.Models;
using HevyMcp.Server.Hevy.Services;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HevyMcp.Server.Tools;

[McpServerToolType]
public class AlternativesTool(ExerciseCatalog catalog, PerformedExercises performed, SavedRoutines routines)
{
    [McpServerTool(Name = "get_exercise_alternatives")]
    [Description(
        "Finds substitutes for an exercise when its equipment is unavailable - a machine is taken, "
        + "a rack is busy, the gym does not have it. "
        + "Use get_routines_for_exercise to identify today's routine unless the user is training "
        + "outside saved routines or today's routine id is already known. Confirm the routine "
        + "with the user even if only one matches, unless they have already identified it. "
        + "Pass the confirmed routine's id as routineId to exclude all its exercises, including "
        + "those not yet performed. If no routine matches or the user is training outside saved "
        + "routines, omit routineId and explain that today's plan was not taken into account. "
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
        string exerciseName,
        [Description(
            "Optional id of today's routine identified or confirmed by the user. "
            + "Use an id returned by get_routines_for_exercise, not a routine title.")]
        string? routineId = null)
    {
        var target = await catalog.FindAsync(exerciseName);

        if (target is null)
            throw new McpException($"Unknown exercise: '{exerciseName}'.");

        var excludedIds = new HashSet<string>();

        if (routineId is not null)
        {
            var routine = (await routines.AllAsync())
                .FirstOrDefault(candidate => candidate.Id == routineId);

            if (routine is null)
                throw new McpException($"Unknown routine: '{routineId}'.");

            excludedIds = routine.Exercises
                .Select(exercise => exercise.ExerciseTemplateId)
                .ToHashSet();
        }

        var sessions = (await performed.FindAsync())
            .ToDictionary(summary => summary.Name, summary => summary.Sessions, StringComparer.OrdinalIgnoreCase);

        var allExercises = (await catalog.AllAsync())
            .Where(template => !excludedIds.Contains(template.Id))
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