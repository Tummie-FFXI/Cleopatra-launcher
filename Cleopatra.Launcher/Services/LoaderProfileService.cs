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
                "Ashita 4 launch command not implemented yet.",

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
        // Ashita 4 support will be implemented after
        // the Windower workflow is complete.

        throw new NotImplementedException(
            "Ashita 4 profile creation is not implemented yet.");
    }

    // ----------------------------------------------------
    // LAUNCH ASHITA 4 PROFILE
    // ----------------------------------------------------

    private static void LaunchAshitaProfile(
        LoaderInstallation loader)
    {
        // Ashita 4 support will be implemented after
        // the Windower workflow is complete.

        throw new NotImplementedException(
            "Ashita 4 profile launching is not implemented yet.");
    }
}