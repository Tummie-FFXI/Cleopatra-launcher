namespace Cleopatra.Launcher.Services;

public class XiLoaderDetectionService
{
    private const string XiLoaderFileName =
        "xiloader.exe";

    // ----------------------------------------------------
    // FIND CLEOPATRA XILOADER
    // ----------------------------------------------------

    public string? FindXiLoader()
    {
        string xiLoaderPath =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    XiLoaderFileName));

        if (!File.Exists(xiLoaderPath))
        {
            return null;
        }

        return xiLoaderPath;
    }
}