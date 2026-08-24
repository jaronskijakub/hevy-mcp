using HevyMcp.Server.Hevy;
namespace HevyMcp.Server.Analysis;

/// <summary>One estimated 1RM measurement at a point in time.</summary>
public record E1RmPoint(DateTimeOffset Date, double E1Rm);

public class Trend
{

    private const double DaysPerMonth = 30.0;

    /// <summary>Estimated 1RM over time, in kg per month.
    public static double? KgPerMonth(IReadOnlyList<E1RmPoint> points)
    {
        if (points.Count < 2) return null;

        var origin = points[0].Date;
        var days = points.Select(point => (point.Date - origin).TotalDays).ToList();

        var meanDay = days.Average();
        var meanE1Rm = points.Average(point => point.E1Rm);

        var covariance = 0.0;
        var variance = 0.0;

        for (var i = 0; i < points.Count; i++)
        {
            var dx = days[i] - meanDay;
            var dy = points[i].E1Rm - meanE1Rm;

            covariance += dx * dy;
            variance += dx * dx;
        }

        if (variance == 0) return null;

        return covariance / variance * DaysPerMonth;
    }
}