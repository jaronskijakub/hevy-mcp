using System.Net.Http.Json;
using System.Text.Json;
using HevyMcp.Server.Hevy.Models;

namespace HevyMcp.Server.Hevy.Services;

public class HevyClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<WorkoutsPage> GetWorkoutPageAsync(int page, int pageSize)
    {
        var url = $"workouts?page={page}&pageSize={pageSize}";
        var result = await http.GetFromJsonAsync<WorkoutsPage>(url, JsonOptions);

        return result
               ?? throw new InvalidOperationException(
                   $"Hevy returned no data for {url}");
    }

    public async Task<ExerciseHistoryResponse> GetExerciseHistoryAsync(string exerciseTemplateId,
        DateTimeOffset? startDate = null)
    {
        var url = $"exercise_history/{exerciseTemplateId}";

        if (startDate is {} from)
            url += $"?start_date={Uri.EscapeDataString(from.ToString("O"))}";

        var result = await http.GetFromJsonAsync<ExerciseHistoryResponse>(url, JsonOptions);

        return result
               ?? throw new InvalidOperationException(
                   $"Hevy returned no data for {url}");
    }

    public async Task<ExerciseTemplatesPage> GetExerciseTemplatePageAsync(int page, int pageSize)
    {
        var url = $"exercise_templates?page={page}&pageSize={pageSize}";
        var result = await http.GetFromJsonAsync<ExerciseTemplatesPage>(url, JsonOptions);

        return result
               ?? throw new InvalidOperationException(
                   $"Hevy returned no data for {url}");
    }
}