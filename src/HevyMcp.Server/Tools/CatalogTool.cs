using System.ComponentModel;
using HevyMcp.Server.Hevy.Services;
using HevyMcp.Server.Tools.Models;
using ModelContextProtocol.Server;

namespace HevyMcp.Server.Tools;

[McpServerToolType]
public class CatalogTool(PerformedExercises performed)
{
    [McpServerTool(Name = "get_exercise_catalog")]
    [Description(
        "Lists the exercises the user has actually performed and logged in Hevy, most frequently "
        + "trained first, with how many sessions each appears in and when it was last performed. "
        + "Call this before get_exercise_progress whenever the exact Hevy title of an exercise is "
        + "not already known - that tool matches titles exactly and fails on a guess. "
        + "Also useful on its own: \"what do I actually train\", \"what have I not done in a while\", "
        + "\"how often do I squat\". "
        + "An empty result means the user has never logged such an exercise, not that the call failed.")]
    public async Task<IReadOnlyList<ExerciseSummary>> GetExerciseCatalog(
        [Description(
            "Optional case-insensitive fragment to filter by - \"row\", \"press\", \"squat\". "
            + "Prefer a short fragment over a full name, since it matches as a substring. "
            + "Omit it to get the user's entire exercise list.")]
        string? query = null)
        => await performed.FindAsync(query);
}