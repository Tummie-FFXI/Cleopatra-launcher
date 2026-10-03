using System.Diagnostics;
using System.Xml.Linq;
using Cleopatra.Launcher.Models;

namespace Cleopatra.Launcher.Services;

public class LoaderProfileService
{
    private const string CleopatraProfileName =
        "Cleopatra";

    private const string DevelopmentServerAddress =
        "192.168.100.36";

    // ----------------------------------------------------
    // ENSURE CLEOPATRA PROFILE
    // ----------------------------------------------------

    public void EnsureCleopatraProfile(
        LoaderInstallation loader,
        string xiLoaderPath)
    {
        if (string.IsNullOrWhiteSpace(xiLoaderPath))
        {
            throw new ArgumentException(
                "XiLoader path is required.",
                nameof(xiLoaderPath));
        }

        switch (loader.LoaderType)
        {
            case LoaderType.Windower4:
                EnsureWindowerProfile(
                    loader,
                    xiLoaderPath);
                break;

            case LoaderType.Ashita4:
                EnsureAshitaProfile(
                    loader,
                    xiLoaderPath);
                break;

            default:
                throw new NotSupportedException(
                    $"Loader type {loader.LoaderType} is not supported.");
        }
    }

    // ----------------------------------------------------
    // GET CLEOPATRA LAUNCH DESCRIPTION
    // ----------------------------------------------------

    public string GetLaunchDescription(
        LoaderInstallation loader)
    {
        return loader.LoaderType switch
        {
            LoaderType.Windower4 =>
                $"\"{Path.Combine(loader.RootPath, "Windower.exe")}\" -p Cleopatra",

            LoaderType.Ashita4 =>
                $"\"{Path.Combine(loader.RootPath, "Ashita-cli.exe")}\" Cleopatra.ini",

            _ =>
                throw new NotSupportedException(
                    $"Loader type {loader.LoaderType} is not supported.")
        };
    }

    // ----------------------------------------------------
    // LAUNCH CLEOPATRA PROFILE
    // ----------------------------------------------------

    public void LaunchCleopatraProfile(
        LoaderInstallation loader)
    {
        switch (loader.LoaderType)
        {
            case LoaderType.Windower4:
                LaunchWindowerProfile(
                    loader);
                break;

            case LoaderType.Ashita4:
                LaunchAshitaProfile(
                    loader);
                break;

            default:
                throw new NotSupportedException(
                    $"Loader type {loader.LoaderType} is not supported.");
        }
    }

    // ----------------------------------------------------
    // WINDOWER 4 PROFILE
    // ----------------------------------------------------

    private static void EnsureWindowerProfile(
        LoaderInstallation loader,
        string xiLoaderPath)
    {
        string settingsPath =
            Path.Combine(
                loader.RootPath,
                "settings.xml");

        if (!File.Exists(settingsPath))
        {
            throw new FileNotFoundException(
                "Windower settings.xml was not found.",
                settingsPath);
        }

        XDocument document =
            XDocument.Load(
                settingsPath,
                LoadOptions.PreserveWhitespace);

        XElement? settings =
            document.Root;

        if (settings == null ||
            settings.Name.LocalName != "settings")
        {
            throw new InvalidDataException(
                "Windower settings.xml is not valid.");
        }

        XElement? cleopatraProfile =
            settings
                .Elements("profile")
                .FirstOrDefault(
                    profile =>
                        string.Equals(
                            (string?)profile.Attribute("name"),
                            CleopatraProfileName,
                            StringComparison.OrdinalIgnoreCase));

        if (cleopatraProfile == null)
        {
            cleopatraProfile =
                new XElement(
                    "profile",
                    new XAttribute(
                        "name",
                        CleopatraProfileName));

            settings.Add(
                cleopatraProfile);
        }

        SetProfileValue(
            cleopatraProfile,
            "args",
            $"--server {DevelopmentServerAddress}");

        SetProfileValue(
            cleopatraProfile,
            "executable",
            Path.GetFullPath(xiLoaderPath));

        document.Save(
            settingsPath);
    }

    // ----------------------------------------------------
    // LAUNCH WINDOWER 4 PROFILE
    // ----------------------------------------------------

    private static void LaunchWindowerProfile(
        LoaderInstallation loader)
    {
        string windowerPath =
            Path.Combine(
                loader.RootPath,
                "Windower.exe");

        if (!File.Exists(windowerPath))
        {
            throw new FileNotFoundException(
                "Windower.exe was not found.",
                windowerPath);
        }

        ProcessStartInfo startInfo =
            new ProcessStartInfo
            {
                FileName =
                    windowerPath,

                WorkingDirectory =
                    loader.RootPath,

                UseShellExecute =
                    true
            };

        startInfo.ArgumentList.Add(
            "-p");

        startInfo.ArgumentList.Add(
            CleopatraProfileName);

        Process.Start(
            startInfo);
    }

    // ----------------------------------------------------
    // SET PROFILE VALUE
    // ----------------------------------------------------

    private static void SetProfileValue(
        XElement profile,
        string elementName,
        string value)
    {
        XElement? element =
            profile.Element(
                elementName);

        if (element == null)
        {
            profile.Add(
                new XElement(
                    elementName,
                    value));

            return;
        }

        element.Value =
            value;
    }

    // ----------------------------------------------------
    // ASHITA 4 PROFILE
    // ----------------------------------------------------

    private static void EnsureAshitaProfile(
        LoaderInstallation loader,
        string xiLoaderPath)
    {
        string bootDirectory =
            Path.Combine(
                loader.RootPath,
                "config",
                "boot");

        Directory.CreateDirectory(
            bootDirectory);

        string profilePath =
            Path.Combine(
                bootDirectory,
                "Cleopatra.ini");

        List<string> lines;

        if (File.Exists(profilePath))
        {
            lines =
                File.ReadAllLines(profilePath)
                    .ToList();
        }
        else
        {
            string examplePath =
                Path.Combine(
                    bootDirectory,
                    "example-privateserver.ini");

            if (File.Exists(examplePath))
            {
                lines =
                    File.ReadAllLines(examplePath)
                        .ToList();
            }
            else
            {
                lines =
                    new List<string>
                    {
                        "[ashita.boot]",
                        "file        =",
                        "command     =",
                        "gamemodule  = ffximain.dll",
                        "script      = default.txt",
                        "args        ="
                    };
            }
        }

        SetIniValue(
            lines,
            "ashita.boot",
            "file",
            Path.GetFullPath(xiLoaderPath));

        SetIniValue(
            lines,
            "ashita.boot",
            "command",
            $"--server {DevelopmentServerAddress}");

        SetIniValue(
            lines,
            "ashita.boot",
            "gamemodule",
            "ffximain.dll");

        SetIniValue(
            lines,
            "ashita.boot",
            "script",
            "default.txt");

        File.WriteAllLines(
            profilePath,
            lines);
    }

    // ----------------------------------------------------
    // LAUNCH ASHITA 4 PROFILE
    // ----------------------------------------------------

    private static void LaunchAshitaProfile(
        LoaderInstallation loader)
    {
        string ashitaPath =
            Path.Combine(
                loader.RootPath,
                "Ashita-cli.exe");

        if (!File.Exists(ashitaPath))
        {
            throw new FileNotFoundException(
                "Ashita-cli.exe was not found.",
                ashitaPath);
        }

        string profilePath =
            Path.Combine(
                loader.RootPath,
                "config",
                "boot",
                "Cleopatra.ini");

        if (!File.Exists(profilePath))
        {
            throw new FileNotFoundException(
                "Cleopatra Ashita profile was not found.",
                profilePath);
        }

        ProcessStartInfo startInfo =
            new ProcessStartInfo
            {
                FileName =
                    ashitaPath,

                WorkingDirectory =
                    loader.RootPath,

                UseShellExecute =
                    true
            };

        startInfo.ArgumentList.Add(
            "Cleopatra.ini");

        Process.Start(
            startInfo);
    }

    // ----------------------------------------------------
    // SET INI VALUE
    // ----------------------------------------------------

    private static void SetIniValue(
        List<string> lines,
        string sectionName,
        string key,
        string value)
    {
        string sectionHeader =
            $"[{sectionName}]";

        int sectionIndex =
            lines.FindIndex(
                line =>
                    line.Trim().Equals(
                        sectionHeader,
                        StringComparison.OrdinalIgnoreCase));

        if (sectionIndex < 0)
        {
            if (lines.Count > 0 &&
                !string.IsNullOrWhiteSpace(lines[^1]))
            {
                lines.Add(
                    string.Empty);
            }

            lines.Add(
                sectionHeader);

            lines.Add(
                $"{key} = {value}");

            return;
        }

        int nextSectionIndex =
            lines.FindIndex(
                sectionIndex + 1,
                line =>
                {
                    string trimmed =
                        line.Trim();

                    return trimmed.StartsWith("[") &&
                           trimmed.EndsWith("]");
                });

        if (nextSectionIndex < 0)
        {
            nextSectionIndex =
                lines.Count;
        }

        for (int i = sectionIndex + 1;
             i < nextSectionIndex;
             i++)
        {
            string trimmed =
                lines[i].Trim();

            if (string.IsNullOrWhiteSpace(trimmed) ||
                trimmed.StartsWith(";") ||
                trimmed.StartsWith("#"))
            {
                continue;
            }

            int equalsIndex =
                trimmed.IndexOf('=');

            if (equalsIndex < 0)
            {
                continue;
            }

            string existingKey =
                trimmed[..equalsIndex]
                    .Trim();

            if (!existingKey.Equals(
                key,
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            lines[i] =
                $"{key} = {value}";

            return;
        }

        lines.Insert(
            nextSectionIndex,
            $"{key} = {value}");
    }
}