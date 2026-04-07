using System.IO;
using System.Text.Json;

namespace OnlineCourses.Desktop.Infrastructure;

public static class DesktopSettingsLoader
{
    private const string DefaultApiBaseUrl = "http://localhost:5064/";
    private const string SettingsFileName = "desktopsettings.json";
    private const string ApiUrlEnvironmentVariable = "ONLINE_COURSES_API_URL";

    public static DesktopSettings Load()
    {
        var environmentValue = Environment.GetEnvironmentVariable(ApiUrlEnvironmentVariable);
        if (TryNormalizeUrl(environmentValue, out var environmentUrl))
        {
            return new DesktopSettings { ApiBaseUrl = environmentUrl };
        }

        var settingsPath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);
        if (!File.Exists(settingsPath))
        {
            return new DesktopSettings { ApiBaseUrl = DefaultApiBaseUrl };
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<DesktopSettings>(json);
            if (settings is not null && TryNormalizeUrl(settings.ApiBaseUrl, out var configuredUrl))
            {
                return new DesktopSettings { ApiBaseUrl = configuredUrl };
            }
        }
        catch
        {
        }

        return new DesktopSettings { ApiBaseUrl = DefaultApiBaseUrl };
    }

    private static bool TryNormalizeUrl(string? value, out string normalizedUrl)
    {
        normalizedUrl = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = value.Trim();
        if (!candidate.EndsWith('/'))
        {
            candidate += "/";
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            return false;
        }

        normalizedUrl = uri.ToString();
        return true;
    }
}
