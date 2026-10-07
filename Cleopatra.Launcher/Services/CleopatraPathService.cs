namespace Cleopatra.Launcher.Services;

public class CleopatraPathService
{
    // ----------------------------------------------------
    // CLEOPATRA DIRECTORY
    // ----------------------------------------------------

    public string GetCleopatraRoot()
    {
        return Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "data",
                "overlays",
                "Cleopatra"));
    }

    public void EnsureCleopatraDirectory()
    {
        Directory.CreateDirectory(
            GetCleopatraRoot());
    }

    // ----------------------------------------------------
    // SAFE DESTINATION PATH
    // ----------------------------------------------------

    public string GetDestinationPath(
        string relativePath)
    {
        string cleopatraRoot =
            Path.GetFullPath(
                GetCleopatraRoot());

        string destinationPath =
            Path.GetFullPath(
                Path.Combine(
                    cleopatraRoot,
                    relativePath));

        string rootWithSeparator =
            cleopatraRoot.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        // Prevent manifest paths such as ../../
        // from escaping Cleopatra's overlay directory.
        if (!destinationPath.StartsWith(
            rootWithSeparator,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Invalid manifest path.");
        }

        return destinationPath;
    }
}