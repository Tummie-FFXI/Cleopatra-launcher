using System.Xml.Linq;
using Microsoft.Win32;
using Cleopatra.Launcher.Models;

namespace Cleopatra.Launcher.Services;

public class XiLoaderDetectionService
{
    private const string XiLoaderFileName =
        "xiloader.exe";

    private const string CleopatraProfileName =
        "Cleopatra";

    // ----------------------------------------------------
    // FIND XILOADER
    // ----------------------------------------------------

    public string? FindXiLoader(
        LoaderInstallation loader)
    {
        if (loader == null)
        {
            throw new ArgumentNullException(
                nameof(loader));
        }

        // --------------------------------------------
        // FIRST:
        // Ask the selected loader where XiLoader is.
        // --------------------------------------------

        string? loaderConfiguredPath =
            loader.LoaderType switch
            {
                LoaderType.Windower4 =>
                    FindXiLoaderFromWindower(loader),

                LoaderType.Ashita4 =>
                    FindXiLoaderFromAshita(loader),

                _ =>
                    null
            };

        if (!string.IsNullOrWhiteSpace(
                loaderConfiguredPath))
        {
            return loaderConfiguredPath;
        }

        // --------------------------------------------
        // FALLBACK:
        // Look for XiLoader in PlayOnline locations.
        // --------------------------------------------

        if (OperatingSystem.IsWindows())
        {
            string? fallbackPath =
                FindXiLoaderFromPlayOnline();

            if (!string.IsNullOrWhiteSpace(
                    fallbackPath))
            {
                return fallbackPath;
            }
        }

        return null;
    }

    // ----------------------------------------------------
    // WINDOWER 4
    // ----------------------------------------------------

    private static string? FindXiLoaderFromWindower(
        LoaderInstallation loader)
    {
        string settingsPath =
            Path.Combine(
                loader.RootPath,
                "settings.xml");

        if (!File.Exists(settingsPath))
        {
            return null;
        }

        XDocument document;

        try
        {
            document =
                XDocument.Load(
                    settingsPath);
        }
        catch
        {
            return null;
        }

        XElement? settings =
            document.Root;

        if (settings == null ||
            !string.Equals(
                settings.Name.LocalName,
                "settings",
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // --------------------------------------------
        // LOOK THROUGH EXISTING WINDOWER PROFILES
        //
        // Cleopatra's own profile is intentionally
        // ignored. We want to discover XiLoader from
        // an existing user configuration rather than
        // trusting a path Cleopatra previously wrote.
        // --------------------------------------------

        foreach (XElement profile in
                 settings.Elements("profile"))
        {
            string? profileName =
                (string?)profile.Attribute("name");

            if (string.Equals(
                profileName,
                CleopatraProfileName,
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            XElement? executableElement =
                profile.Element("executable");

            if (executableElement == null)
            {
                continue;
            }

            string executablePath =
                executableElement.Value.Trim();

            if (IsValidXiLoaderPath(
                executablePath))
            {
                return Path.GetFullPath(
                    executablePath);
            }
        }

        return null;
    }

    // ----------------------------------------------------
    // ASHITA 4
    // ----------------------------------------------------
    //
    // We are intentionally leaving Ashita discovery
    // conservative until we test it against a real
    // Ashita 4 installation.
    //
    // The service is already structured so Ashita
    // detection can be added here without changing
    // LauncherService or the GUI.
    // ----------------------------------------------------

    private static string? FindXiLoaderFromAshita(
        LoaderInstallation loader)
    {
        return null;
    }

    // ----------------------------------------------------
    // VALIDATE XILOADER PATH
    // ----------------------------------------------------

    private static bool IsValidXiLoaderPath(
        string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        string fileName;

        try
        {
            fileName =
                Path.GetFileName(path);
        }
        catch
        {
            return false;
        }

        if (!string.Equals(
            fileName,
            XiLoaderFileName,
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return File.Exists(path);
    }

    // ----------------------------------------------------
    // PLAYONLINE FALLBACK
    // ----------------------------------------------------

    private static string? FindXiLoaderFromPlayOnline()
    {
        // --------------------------------------------
        // TRY REGISTRY LOCATIONS
        // --------------------------------------------

        foreach (string playOnlinePath in
                 GetPlayOnlinePathsFromRegistry())
        {
            string? xiLoaderPath =
                FindXiLoaderInPlayOnlinePath(
                    playOnlinePath);

            if (xiLoaderPath != null)
            {
                return xiLoaderPath;
            }
        }

        // --------------------------------------------
        // TRY COMMON INSTALL LOCATIONS
        // --------------------------------------------

        foreach (string playOnlinePath in
                 GetCommonPlayOnlinePaths())
        {
            string? xiLoaderPath =
                FindXiLoaderInPlayOnlinePath(
                    playOnlinePath);

            if (xiLoaderPath != null)
            {
                return xiLoaderPath;
            }
        }

        return null;
    }

    // ----------------------------------------------------
    // FIND XILOADER INSIDE PLAYONLINE PATH
    // ----------------------------------------------------

    private static string? FindXiLoaderInPlayOnlinePath(
        string playOnlinePath)
    {
        if (string.IsNullOrWhiteSpace(
            playOnlinePath))
        {
            return null;
        }

        string normalizedPath;

        try
        {
            normalizedPath =
                Path.GetFullPath(
                    playOnlinePath);
        }
        catch
        {
            return null;
        }

        string rootCandidate =
            Path.Combine(
                normalizedPath,
                XiLoaderFileName);

        if (IsValidXiLoaderPath(
            rootCandidate))
        {
            return rootCandidate;
        }

        string nestedCandidate =
            Path.Combine(
                normalizedPath,
                "SquareEnix",
                "PlayOnlineViewer",
                XiLoaderFileName);

        if (IsValidXiLoaderPath(
            nestedCandidate))
        {
            return nestedCandidate;
        }

        string viewerCandidate =
            Path.Combine(
                normalizedPath,
                "PlayOnlineViewer",
                XiLoaderFileName);

        if (IsValidXiLoaderPath(
            viewerCandidate))
        {
            return viewerCandidate;
        }

        return null;
    }

    // ----------------------------------------------------
    // REGISTRY PLAYONLINE PATHS
    // ----------------------------------------------------

    private static IEnumerable<string>
        GetPlayOnlinePathsFromRegistry()
    {
        if (!OperatingSystem.IsWindows())
        {
            yield break;
        }

        RegistryView[] registryViews =
        {
            RegistryView.Registry64,
            RegistryView.Registry32
        };

        string[] registryKeys =
        {
            @"SOFTWARE\PlayOnlineUS\InstallFolder",
            @"SOFTWARE\WOW6432Node\PlayOnlineUS\InstallFolder"
        };

        foreach (RegistryView view in registryViews)
        {
            using RegistryKey baseKey =
                RegistryKey.OpenBaseKey(
                    RegistryHive.LocalMachine,
                    view);

            foreach (string keyPath in registryKeys)
            {
                using RegistryKey? key =
                    baseKey.OpenSubKey(
                        keyPath);

                if (key == null)
                {
                    continue;
                }

                foreach (string valueName in
                         key.GetValueNames())
                {
                    object? value =
                        key.GetValue(
                            valueName);

                    if (value is not string path ||
                        string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    yield return path;
                }
            }
        }
    }

    // ----------------------------------------------------
    // COMMON PLAYONLINE LOCATIONS
    // ----------------------------------------------------

    private static IEnumerable<string>
        GetCommonPlayOnlinePaths()
    {
        string? programFilesX86 =
            Environment.GetEnvironmentVariable(
                "ProgramFiles(x86)");

        string? programFiles =
            Environment.GetEnvironmentVariable(
                "ProgramFiles");

        if (!string.IsNullOrWhiteSpace(
            programFilesX86))
        {
            yield return Path.Combine(
                programFilesX86,
                "PlayOnline",
                "SquareEnix",
                "PlayOnlineViewer");

            yield return Path.Combine(
                programFilesX86,
                "SquareEnix",
                "PlayOnlineViewer");
        }

        if (!string.IsNullOrWhiteSpace(
            programFiles))
        {
            yield return Path.Combine(
                programFiles,
                "PlayOnline",
                "SquareEnix",
                "PlayOnlineViewer");

            yield return Path.Combine(
                programFiles,
                "SquareEnix",
                "PlayOnlineViewer");
        }
    }
}