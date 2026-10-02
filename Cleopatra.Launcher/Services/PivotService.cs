namespace Cleopatra.Launcher.Services;

public class PivotService
{
    private readonly string _pivotRoot;

    public PivotService(string pivotRoot)
    {
        _pivotRoot = pivotRoot;
    }

    public string GetCleopatraRoot()
    {
        return Path.Combine(
            _pivotRoot,
            "Cleopatra");
    }

    public string GetDestinationPath(string relativePath)
    {
        string cleopatraRoot =
            Path.GetFullPath(GetCleopatraRoot());

        string destinationPath =
            Path.GetFullPath(
                Path.Combine(
                    cleopatraRoot,
                    relativePath));

        // Prevent a manifest path from escaping
        // Cleopatra's Pivot directory.
        string rootWithSeparator =
            cleopatraRoot.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!destinationPath.StartsWith(
            rootWithSeparator,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Invalid manifest path.");
        }

        return destinationPath;
    }

    public void EnsureCleopatraDirectory()
    {
        Directory.CreateDirectory(
            GetCleopatraRoot());
    }
}