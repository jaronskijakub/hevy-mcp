using System.ComponentModel;
using HevyMcp.Server.Hevy;
using ModelContextProtocol.Server;

namespace HevyMcp.Server.Tools;

[McpServerToolType]
public class HevyDebugTools(HevyClient hevy)
{
    [McpServerTool(Name = "hevy_debug_last_workout")]
    [Description("Temporary. Returns a one-line summary of the most recent workout. "
                 + "Used only to verify that the Hevy API connection works.")]
    public async Task<string> GetLastWorkout()
    {
        var page = await hevy.GetWorkoutPageAsync(page: 1, pageSize: 1);
        var workout = page.Workouts[0];
        var setCount = workout.Exercises.Sum(exercise => exercise.Sets.Count);

        return $"{workout.Title} - {workout.StartTime:yyyy-MM-dd} - "
               + $"{workout.Exercises.Count} exercises, {setCount} sets";
    }
}