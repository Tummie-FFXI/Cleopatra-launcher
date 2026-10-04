using System.Security.Cryptography;
using Cleopatra.Launcher.Models;

namespace Cleopatra.Launcher.Services;

public class FileUpdater
{
    private readonly HttpClient _client;

    private const int DownloadBufferSize =
        81920;

    public FileUpdater(
        HttpClient client)
    {
        _client =
            client;
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
            CalculateSha256(
                destinationPath);

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
        string destinationPath,
        IProgress<FileDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        string tempPath =
            destinationPath + ".download";

        string? directory =
            Path.GetDirectoryName(
                destinationPath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(
                directory);
        }

        try
        {
            // --------------------------------------------
            // CLEAN UP ABANDONED TEMPORARY DOWNLOAD
            // --------------------------------------------

            if (File.Exists(tempPath))
            {
                File.Delete(
                    tempPath);
            }

            // --------------------------------------------
            // BEGIN HTTP DOWNLOAD
            //
            // ResponseHeadersRead is important here.
            // It prevents HttpClient from buffering the
            // entire DAT before returning control to us.
            // --------------------------------------------

            using HttpResponseMessage response =
                await _client.GetAsync(
                    updateFile.Url,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

            response.EnsureSuccessStatusCode();

            long? totalBytes =
                response.Content.Headers.ContentLength;

            byte[] buffer =
                new byte[DownloadBufferSize];

            long totalBytesRead =
                0;

            int bytesRead;

            // Initial progress notification.

            progress?.Report(
                new FileDownloadProgress(
                    totalBytesRead,
                    totalBytes,
                    CalculatePercent(
                        totalBytesRead,
                        totalBytes)));

            // --------------------------------------------
            // DOWNLOAD TO TEMPORARY FILE
            // --------------------------------------------
            //
            // Keep the download streams inside their own
            // scope. This guarantees the FileStream is
            // closed before we reopen the temporary file
            // for SHA-256 verification or move it into
            // place on Windows.
            // --------------------------------------------

            await using (
                Stream downloadStream =
                    await response.Content.ReadAsStreamAsync(
                        cancellationToken))
            {
                await using (
                    FileStream fileStream =
                        new FileStream(
                            tempPath,
                            FileMode.Create,
                            FileAccess.Write,
                            FileShare.None,
                            DownloadBufferSize,
                            useAsync: true))
                {
                    // ----------------------------------------
                    // STREAM FILE TO DISK
                    // ----------------------------------------

                    while ((bytesRead =
                        await downloadStream.ReadAsync(
                            buffer.AsMemory(
                                0,
                                buffer.Length),
                            cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(
                            buffer.AsMemory(
                                0,
                                bytesRead),
                            cancellationToken);

                        totalBytesRead +=
                            bytesRead;

                        progress?.Report(
                            new FileDownloadProgress(
                                totalBytesRead,
                                totalBytes,
                                CalculatePercent(
                                    totalBytesRead,
                                    totalBytes)));
                    }

                    await fileStream.FlushAsync(
                        cancellationToken);
                }
            }

            // --------------------------------------------
            // VERIFY SHA-256
            // --------------------------------------------
            //
            // The download FileStream has been disposed
            // before CalculateSha256 opens tempPath.
            // --------------------------------------------

            string downloadedHash =
                CalculateSha256(
                    tempPath);

            if (!downloadedHash.Equals(
                updateFile.Sha256,
                StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(
                    tempPath);

                return false;
            }

            // --------------------------------------------
            // VERIFICATION SUCCEEDED
            //
            // Only now do we replace the actual
            // Cleopatra DAT.
            // --------------------------------------------

            File.Move(
                tempPath,
                destinationPath,
                true);

            // Make sure the GUI receives a final 100%
            // notification when the server supplied a
            // Content-Length.

            if (totalBytes.HasValue)
            {
                progress?.Report(
                    new FileDownloadProgress(
                        totalBytesRead,
                        totalBytes,
                        100.0));
            }

            return true;
        }
        catch
        {
            // --------------------------------------------
            // REMOVE INCOMPLETE TEMPORARY DOWNLOAD
            // --------------------------------------------

            if (File.Exists(tempPath))
            {
                File.Delete(
                    tempPath);
            }

            throw;
        }
    }

    // ----------------------------------------------------
    // CALCULATE DOWNLOAD PERCENT
    // ----------------------------------------------------

    private static double? CalculatePercent(
        long bytesDownloaded,
        long? totalBytes)
    {
        if (!totalBytes.HasValue ||
            totalBytes.Value <= 0)
        {
            return null;
        }

        double percent =
            (double)bytesDownloaded /
            totalBytes.Value *
            100.0;

        return Math.Clamp(
            percent,
            0.0,
            100.0);
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
            Path.GetFullPath(
                cleopatraRoot);

        string fullPath =
            Path.GetFullPath(
                destinationPath);

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

        File.Delete(
            fullPath);

        // Clean up empty folders left behind.

        RemoveEmptyParentDirectories(
            Path.GetDirectoryName(
                fullPath),
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
                Path.GetFullPath(
                    directory);

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
                .EnumerateFileSystemEntries(
                    fullDirectory)
                .Any())
            {
                break;
            }

            Directory.Delete(
                fullDirectory);

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
            File.OpenRead(
                filePath);

        byte[] hash =
            sha256.ComputeHash(
                stream);

        return Convert
            .ToHexString(hash)
            .ToLowerInvariant();
    }
}

// ----------------------------------------------------
// FILE DOWNLOAD PROGRESS
// ----------------------------------------------------
//
// This object is reported while an individual DAT
// file is downloading.
//
// TotalBytes and Percent can be null because an HTTP
// server is not required to provide Content-Length.
// ----------------------------------------------------

public sealed record FileDownloadProgress(
    long BytesDownloaded,
    long? TotalBytes,
    double? Percent);