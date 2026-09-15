using System.ComponentModel;
using HevyMcp.Server.Hevy.Models;
using HevyMcp.Server.Hevy.Services;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HevyMcp.Server.Tools;

[McpServerToolType]
public class RoutineTool(ExerciseCatalog catalog, SavedRoutines routines)
{
    [McpServerTool(Name = "get_routines_for_exercise")]
    [Description(
        "Lists saved Hevy routines containing an exercise, including each routine's id, title "
        + "and all its exercises. Use this to identify today's routine when finding substitutes. "
        + "Ask which routine the user is doing today, even if only one matches, unless the user "
        + "has already identified or confirmed today's routine in the conversation. "
        + "An empty list means no saved routine contains this exercise. If no routine matches "
        + "or the user is training outside saved routines, proceed without routine exclusions "
        + "and explain that today's plan was not taken into account.")]
    public async Task<IReadOnlyList<Routine>> GetRoutinesForExercise(
        [Description(
            "Exercise title exactly as it appears in Hevy. "
            + "Use get_exercise_catalog first when the exact title is not known.")]
        string exerciseName)
    {
        var target = await catalog.FindAsync(exerciseName);

        if (target is null)
            throw new McpException($"Unknown exercise: '{exerciseName}'.");

        return await routines.FindByExerciseAsync(target.Id);
    }
}