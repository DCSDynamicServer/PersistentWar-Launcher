using System.Text.Json;
using DcsWarLauncher.Domain;

namespace DcsWarLauncher.Infrastructure;

public sealed class StateStore(IWebHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _path = Path.Combine(environment.ContentRootPath, "Data", "war-state.json");

    public async Task<WarState> LoadAsync()
    {
        if (!File.Exists(_path))
        {
            var initial = WarState.CreateDefault();
            await SaveAsync(initial);
            return initial;
        }

        await using var stream = File.OpenRead(_path);
        var state = await JsonSerializer.DeserializeAsync<WarState>(stream, JsonOptions) ?? WarState.CreateDefault();
        return state.Normalize();
    }

    public async Task SaveAsync(WarState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, state, JsonOptions);
    }
}
