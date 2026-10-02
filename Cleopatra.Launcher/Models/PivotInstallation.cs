namespace Cleopatra.Launcher.Models;

public class PivotInstallation
{
    public LoaderType LoaderType { get; set; }

    public string LoaderRoot { get; set; } = "";

    public string PivotRoot { get; set; } = "";

    public string DatRoot { get; set; } = "";

    public string CleopatraRoot { get; set; } = "";

    public string ConfigurationPath { get; set; } = "";
}