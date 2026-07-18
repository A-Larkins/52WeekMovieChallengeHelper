using Microsoft.Extensions.Configuration;

/// <summary>
/// Finds appsettings.json so the console app and the GUI resolve API keys the
/// same way, wherever each one happens to be running from.
/// </summary>
public static class AppConfiguration
{
    private static readonly Lazy<IConfigurationRoot> Config = new(Build);

    /// <summary>Candidate config locations, highest priority first.</summary>
    private static IEnumerable<string> CandidatePaths()
    {
        // Beside the executable: covers `dotnet run`, a published folder, and
        // the .app bundle (Contents/MacOS).
        yield return Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        // Where publish.sh installs it alongside the 52wmovie console build.
        yield return "/usr/local/bin/appsettings.json";
    }

    private static IConfigurationRoot Build()
    {
        var builder = new ConfigurationBuilder();
        var found = false;

        // Added lowest priority first, since later sources win.
        foreach (var path in CandidatePaths().Reverse())
        {
            if (!File.Exists(path)) continue;
            builder.AddJsonFile(path, optional: true);
            found = true;
        }

        if (!found)
            throw new FileNotFoundException(
                "Couldn't find appsettings.json. Looked in:" + Environment.NewLine +
                string.Join(Environment.NewLine, CandidatePaths()));

        return builder.Build();
    }

    /// <summary>Reads a key, failing with a usable message rather than a null API key.</summary>
    public static string RequireKey(string key)
    {
        var value = Config.Value[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"'{key}' is missing from appsettings.json.");

        return value;
    }
}
