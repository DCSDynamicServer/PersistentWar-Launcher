using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Infrastructure;

public sealed class AutomationLogService(IWebHostEnvironment environment, IConfiguration configuration)
{
    private readonly string _logPath = Path.Combine(DataPathResolver.GetDataRoot(environment, configuration), "Logs", "automation.log");

    public async Task AppendAsync(string message, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
        var line = $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}";
        await File.AppendAllTextAsync(_logPath, line, cancellationToken);
    }

    public AutomationLogSnapshot GetSnapshot(int maxLines = 25)
    {
        if (!File.Exists(_logPath))
        {
            return new AutomationLogSnapshot(false, _logPath, []);
        }

        var lines = File.ReadLines(_logPath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .TakeLast(Math.Clamp(maxLines, 1, 200))
            .ToList();

        return new AutomationLogSnapshot(true, _logPath, lines);
    }
}
