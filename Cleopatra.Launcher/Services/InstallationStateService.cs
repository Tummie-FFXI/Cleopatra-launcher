using System.Text.Json;
using Cleopatra.Launcher.Models;

namespace Cleopatra.Launcher.Services;

public class InstallationStateService
{
    private const string StateFileName =
        ".cleopatra-installed.json";

    public InstalledManifest Load(
        string cleopatraRoot)
    {
        string statePath =
            Path.Combine(
                cleopatraRoot,
                StateFileName);

        if (!File.Exists(statePath))
        {
            return new InstalledManifest();
        }

        string json =
            File.ReadAllText(statePath);

        return JsonSerializer.Deserialize<InstalledManifest>(
                   json,
                   new JsonSerializerOptions
                   {
                       PropertyNameCaseInsensitive = true
                   })
               ?? new InstalledManifest();
    }

    public void Save(
        string cleopatraRoot,
        UpdateManifest manifest)
    {
        Directory.CreateDirectory(
            cleopatraRoot);

        InstalledManifest installed =
            new InstalledManifest
            {
                Version = manifest.Version,

                Files = manifest.Files
                    .Select(file => file.Path)
                    .ToList()
            };

        string json =
            JsonSerializer.Serialize(
                installed,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        string statePath =
            Path.Combine(
                cleopatraRoot,
                StateFileName);

        File.WriteAllText(
            statePath,
            json);
    }

    public List<string> GetObsoleteFiles(
        InstalledManifest installed,
        UpdateManifest current)
    {
        HashSet<string> currentFiles =
            current.Files
                .Select(file => file.Path)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        return installed.Files
            .Where(
                file => !currentFiles.Contains(file))
            .ToList();
    }
}