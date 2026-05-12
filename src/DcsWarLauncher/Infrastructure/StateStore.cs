using System.Text.Json;
using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Infrastructure;

public sealed class StateStore(IWebHostEnvironment environment, IConfiguration? configuration = null)
{
    private const int MaxBackupFiles = 30;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _path = Path.Combine(GetDataRoot(environment, configuration), "war-state.json");
    private readonly string _backupPath = Path.Combine(GetDataRoot(environment, configuration), "Backups");

    public async Task<WarState> LoadAsync()
    {
        if (!File.Exists(_path))
        {
            var initial = WarState.CreateDefault();
            await SaveAsync(initial, createBackup: false);
            return initial;
        }

        try
        {
            await using var stream = File.OpenRead(_path);
            var state = await JsonSerializer.DeserializeAsync<WarState>(stream, JsonOptions) ?? WarState.CreateDefault();
            return state.Normalize();
        }
        catch (JsonException)
        {
            await BackupExistingStateAsync("corrupt");
            var initial = WarState.CreateDefault();
            await SaveAsync(initial, createBackup: false);
            return initial;
        }
    }

    public Task SaveAsync(WarState state) => SaveAsync(state, createBackup: true);

    private async Task SaveAsync(WarState state, bool createBackup)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        if (createBackup && File.Exists(_path))
        {
            await BackupExistingStateAsync("backup");
        }

        state = state.Normalize();
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, state, JsonOptions);
    }

    private Task BackupExistingStateAsync(string label)
    {
        Directory.CreateDirectory(_backupPath);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        var backupFileName = $"war-state-{label}-{timestamp}-{Guid.NewGuid():N}.json";
        var backupFilePath = Path.Combine(_backupPath, backupFileName);
        File.Copy(_path, backupFilePath, overwrite: false);
        PruneOldBackups();
        return Task.CompletedTask;
    }

    private void PruneOldBackups()
    {
        var backups = Directory
            .GetFiles(_backupPath, "war-state-*.json")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.CreationTimeUtc)
            .ThenByDescending(file => file.Name)
            .Skip(MaxBackupFiles);

        foreach (var backup in backups)
        {
            backup.Delete();
        }
    }

    private static string GetDataRoot(IWebHostEnvironment environment, IConfiguration? configuration) =>
        configuration is null
            ? DataPathResolver.GetDataRoot(environment)
            : DataPathResolver.GetDataRoot(environment, configuration);
}
