using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace Games.Pages;

public partial class About : ComponentBase
{
    protected VersionInfo VersionInfo { get; private set; } = new();

    protected override void OnInitialized()
    {
        VersionInfo = new VersionInfo();
    }
}

public class VersionInfo
{
    public string Version { get; }
    public string BuildTime { get; }
    public string Framework { get; }

    public VersionInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Get version from assembly
        Version = assembly.GetName().Version?.ToString() ?? "1.0.0.0";

        // Get framework version
        Framework = Environment.Version.ToString();

        // Get build time from assembly (if available) or use current time as fallback
        BuildTime = GetBuildTime(assembly);
    }

    private static string GetBuildTime(Assembly assembly)
    {
        try
        {
            // Try to get build time from assembly metadata
            var buildTimeAttribute = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attr => attr.Key == "BuildTime");
            if (buildTimeAttribute != null)
            {
                if (DateTime.TryParse(buildTimeAttribute.Value, out var buildTime))
                {
                    return buildTime.ToString("yyyy-MM-dd HH:mm:ss UTC");
                }
            }

            // Fallback: use assembly creation time
            var location = assembly.Location;
            if (!string.IsNullOrEmpty(location) && File.Exists(location))
            {
                var creationTime = File.GetCreationTimeUtc(location);
                return creationTime.ToString("yyyy-MM-dd HH:mm:ss UTC");
            }
        }
        catch
        {
            // If anything fails, return current time
        }

        return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
    }
}