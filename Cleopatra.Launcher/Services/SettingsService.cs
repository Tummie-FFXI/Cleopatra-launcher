using System.Text.Json;
using System.Text.Json.Serialization;
using Cleopatra.Launcher.Models;

namespace Cleopatra.Launcher.Services;

public class SettingsService
{
    private const string SettingsFileName =
        "settings.json";

    private readonly string _settingsDirectory;
    private readonly string _settingsPath;

    public SettingsService()
    {
        _settingsDirectory =
            GetSettingsDirectory();

        _settingsPath =
            Path.Combine(
                _settingsDirectory,
                SettingsFileName);
    }

    public string SettingsPath =>
        _settingsPath;

    // ----------------------------------------------------
    // LOAD SETTINGS
    // ----------------------------------------------------

    public LauncherSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new LauncherSettings();
        }

        try
        {
            string json =
                File.ReadAllText(
                    _settingsPath);

            return JsonSerializer.Deserialize<LauncherSettings>(
                       json,
                       GetJsonOptions())
                   ?? new LauncherSettings();
        }
        catch
        {
            // A damaged settings file should not prevent
            // Cleopatra from starting. Treat it as though
            // no settings have been saved.
            return new LauncherSettings();
        }
    }

    // ----------------------------------------------------
    // SAVE SETTINGS
    // ----------------------------------------------------

    public void Save(
        LauncherSettings settings)
    {
        Directory.CreateDirectory(
            _settingsDirectory);

        string json =
            JsonSerializer.Serialize(
                settings,
                GetJsonOptions());

        File.WriteAllText(
            _settingsPath,
            json);
    }

    // ----------------------------------------------------
    // SETTINGS LOCATION
    // ----------------------------------------------------

    private static string GetSettingsDirectory()
    {
        string baseDirectory;

        if (OperatingSystem.IsWindows())
        {
            baseDirectory =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);
        }
        else
        {
            // Development/testing location on macOS/Linux.
            baseDirectory =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile);
        }

        return Path.Combine(
            baseDirectory,
            "CleopatraLauncher");
    }

    // ----------------------------------------------------
    // JSON OPTIONS
    // ----------------------------------------------------

    private static JsonSerializerOptions GetJsonOptions()
    {
        JsonSerializerOptions options =
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };

        options.Converters.Add(
            new JsonStringEnumConverter());

        return options;
    }
}