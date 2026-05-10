using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Campaign;

public static class FrontlineEngine
{
    public static List<FrontlineSegment> BuildFrontlines(IReadOnlyList<ObjectiveState> objectives, int turn)
    {
        var segments = new List<FrontlineSegment>();
        for (var i = 0; i < objectives.Count - 1; i++)
        {
            var left = objectives[i];
            var right = objectives[i + 1];
            var control = (left.Strength + right.Strength) / 2;
            var jitter = ((turn + i) % 3 - 1) * 3;
            segments.Add(new FrontlineSegment(
                $"{left.Name}-{right.Name}",
                Math.Clamp(20 + i * 25, 0, 100),
                Math.Clamp(100 - control + jitter, 0, 100),
                Math.Clamp(42 + i * 20, 0, 100),
                Math.Clamp(100 - control - jitter, 0, 100),
                control >= 55 ? "blue-advancing" : control <= 45 ? "red-advancing" : "static"));
        }

        return segments;
    }
}
