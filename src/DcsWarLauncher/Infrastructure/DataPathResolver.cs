namespace DcsWarLauncher.Infrastructure;

public static class DataPathResolver
{
    public static string GetDataRoot(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configuredDataRoot = configuration["Launcher:DataRoot"];
        return string.IsNullOrWhiteSpace(configuredDataRoot)
            ? GetDataRoot(environment)
            : configuredDataRoot;
    }

    public static string GetDataRoot(IWebHostEnvironment environment)
    {
        var contentData = Path.Combine(environment.ContentRootPath, "Data");
        var contentSourceData = Path.Combine(environment.ContentRootPath, "src", "DcsWarLauncher", "Data");
        if (Directory.Exists(contentData))
        {
            return contentData;
        }

        if (Directory.Exists(contentSourceData))
        {
            return contentSourceData;
        }

        if (!IsBuildOutput(environment.ContentRootPath))
        {
            return contentData;
        }

        var candidates = BuildFallbackDataRoots(environment.ContentRootPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var existing = candidates.FirstOrDefault(Directory.Exists);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        return contentData;
    }

    private static IEnumerable<string> BuildFallbackDataRoots(string contentRoot)
    {
        var current = Environment.CurrentDirectory;
        var baseDirectory = AppContext.BaseDirectory;

        if (IsBuildOutput(contentRoot))
        {
            yield return Path.Combine(current, "Data");
            yield return Path.Combine(baseDirectory, "Data");
            yield return Path.Combine(contentRoot, "Data");
        }
        else
        {
            yield return Path.Combine(contentRoot, "Data");
            yield return Path.Combine(current, "Data");
            yield return Path.Combine(baseDirectory, "Data");
        }

        foreach (var root in WalkRoots(contentRoot, current, baseDirectory))
        {
            if (root.Equals(contentRoot, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return Path.Combine(root, "src", "DcsWarLauncher", "Data");
        }
    }

    private static bool IsBuildOutput(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/bin/Debug/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/bin/Release/", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> WalkRoots(params string[] paths)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var current = new DirectoryInfo(path);
            while (current is not null)
            {
                if (seen.Add(current.FullName))
                {
                    yield return current.FullName;
                }

                current = current.Parent;
            }
        }
    }
}
