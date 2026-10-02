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

            // Verified. Move it into the Pivot directory.
            File.Move(
                tempPath,
                destinationPath,
                true);

            return true;
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

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