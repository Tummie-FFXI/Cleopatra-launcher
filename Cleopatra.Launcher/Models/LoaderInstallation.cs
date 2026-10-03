namespace Cleopatra.Launcher.Models;

public class LoaderInstallation
{
    public LoaderType LoaderType { get; set; }

    public string RootPath { get; set; } = "";

    public string DisplayName { get; set; } = "";
}