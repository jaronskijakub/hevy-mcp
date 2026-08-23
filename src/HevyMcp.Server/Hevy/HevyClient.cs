using System.Net.Http.Json;
using System.Text.Json;

namespace HevyMcp.Server.Hevy;

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
}