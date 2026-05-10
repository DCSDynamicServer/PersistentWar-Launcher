namespace DcsWarLauncher.Infrastructure;

public static class DataPathResolver
{
    public static string GetDataRoot(IWebHostEnvironment environment)
    {
        var contentData = Path.Combine(environment.ContentRootPath, "Data");
        var currentData = Path.Combine(Environment.CurrentDirectory, "Data");

        if (IsBuildOutput(environment.ContentRootPath) && Directory.Exists(currentData))
        {
            return currentData;
        }

        if (Directory.Exists(contentData))
        {
            return contentData;
        }

        return Directory.Exists(currentData) ? currentData : contentData;
    }

    private static bool IsBuildOutput(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/bin/Debug/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/bin/Release/", StringComparison.OrdinalIgnoreCase);
    }
}
