using System.Security.Cryptography;
using Cleopatra.Launcher.Models;

namespace Cleopatra.Launcher.Services;

public class FileUpdater
{
    private readonly HttpClient _client;

    public FileUpdater(HttpClient client)
    {
        _client = client;
    }

    // ----------------------------------------------------
    // CHECK FILE STATUS
    // ----------------------------------------------------

    public string GetFileStatus(
        UpdateFile updateFile,
        string destinationPath)
    {
        if (!File.Exists(destinationPath))
        {
            return "MISSING";
        }

        string localHash =
            CalculateSha256(destinationPath);

        if (localHash.Equals(
            updateFile.Sha256,
            StringComparison.OrdinalIgnoreCase))
        {
            return "CURRENT";
        }

        return "OUTDATED";
    }

    // ----------------------------------------------------
    // DOWNLOAD AND VERIFY
    // ----------------------------------------------------

    public async Task<bool> DownloadAndVerifyAsync(
        UpdateFile updateFile,
        string destinationPath)
    {
        string tempPath =
            destinationPath + ".download";

        string? directory =
            Path.GetDirectoryName(destinationPath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            // Clean up an abandoned temporary download.
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            // Download the new file.
            byte[] fileData =
                await _client.GetByteArrayAsync(
                    updateFile.Url);

            // Write to a temporary file first.
            await File.WriteAllBytesAsync(
                tempPath,
                fileData);

            // Verify SHA-256 before replacing anything.
            string downloadedHash =
                CalculateSha256(tempPath);

            if (!downloadedHash.Equals(
                updateFile.Sha256,
                StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(tempPath);

                return false;
            }

            // Verification succeeded.
            // Replace the real file.
            File.Move(
                tempPath,
                destinationPath,
                true);

            return true;
        }
        catch
        {
            // Remove an incomplete temporary download.
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    // ----------------------------------------------------
    // REMOVE OBSOLETE CLEOPATRA FILE
    // ----------------------------------------------------

    public bool RemoveObsoleteFile(
        string destinationPath,
        string cleopatraRoot)
    {
        if (!File.Exists(destinationPath))
        {
            return false;
        }

        string fullRoot =
            Path.GetFullPath(cleopatraRoot);

        string fullPath =
            Path.GetFullPath(destinationPath);

        string rootWithSeparator =
            fullRoot.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        // Safety check:
        // Cleopatra may NEVER delete anything outside
        // its own Pivot overlay directory.
        if (!fullPath.StartsWith(
            rootWithSeparator,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Refusing to delete a file outside the Cleopatra directory.");
        }

        File.Delete(fullPath);

        // Clean up empty folders left behind.
        RemoveEmptyParentDirectories(
            Path.GetDirectoryName(fullPath),
            fullRoot);

        return true;
    }

    // ----------------------------------------------------
    // REMOVE EMPTY DIRECTORIES
    // ----------------------------------------------------

    private static void RemoveEmptyParentDirectories(
        string? directory,
        string cleopatraRoot)
    {
        while (!string.IsNullOrEmpty(directory))
        {
            string fullDirectory =
                Path.GetFullPath(directory);

            // NEVER delete the Cleopatra root itself.
            if (fullDirectory.Equals(
                cleopatraRoot,
                StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (!Directory.Exists(fullDirectory))
            {
                directory =
                    Path.GetDirectoryName(
                        fullDirectory);

                continue;
            }

            // If the directory contains anything,
            // leave it alone and stop climbing.
            if (Directory
                .EnumerateFileSystemEntries(fullDirectory)
                .Any())
            {
                break;
            }

            Directory.Delete(fullDirectory);

            directory =
                Path.GetDirectoryName(
                    fullDirectory);
        }
    }

    // ----------------------------------------------------
    // SHA-256
    // ----------------------------------------------------

    private static string CalculateSha256(
        string filePath)
    {
        using SHA256 sha256 =
            SHA256.Create();

        using FileStream stream =
            File.OpenRead(filePath);

        byte[] hash =
            sha256.ComputeHash(stream);

        return Convert
            .ToHexString(hash)
            .ToLowerInvariant();
    }
}