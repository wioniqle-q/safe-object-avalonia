using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexpLock.Interfaces;
using SafeObject.Core.Interfaces;
using SafeObject.Core.Models;
using SafeObject.Core.Services;
using SafeObject.Core.Services.Factory;

namespace NexpLock.Services;

public sealed class EncryptionService(IStorageService storageService) : IEncryptionService
{
    private static readonly string BaseDirectory = Environment.CurrentDirectory;

    public async Task EncryptFileAsync(string filePath, string key,
        IProgress<(double percentage, string message)> progress, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var sourcePath = Path.Combine(BaseDirectory, filePath);
        if (File.Exists(sourcePath) is not true)
            throw new FileNotFoundException(Constants.Exception.FileNotFound, sourcePath);

        var sourceFileName = Path.GetFileName(filePath);
        if (IsFileEncrypted(sourceFileName.AsSpan()))
            throw new InvalidOperationException("File is already encrypted.");

        var fileId = Guid.CreateVersion7().ToString();
        var encryptedFilePath =
            Path.Combine(BaseDirectory, Constants.Vault.EncryptedFolder, $"{sourceFileName}_{fileId}");

        await storageService.EncryptFileAsync(
            new FileProcessingRequest(fileId, sourcePath, encryptedFilePath), key, token);

        if (File.Exists(encryptedFilePath))
        {
            await using var fileStream = CreateFileStream(encryptedFilePath, FileMode.Open, FileAccess.Write, null);
            await fileStream.FlushAsync(token);
        }
    }

    public async Task DecryptFileAsync(string filePath, string key,
        IProgress<(double percentage, string message)> progress, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var sourcePath = Path.Combine(BaseDirectory, filePath);
        if (File.Exists(sourcePath) is not true)
            throw new FileNotFoundException(Constants.Exception.FileNotFound, sourcePath);

        var fileName = Path.GetFileName(sourcePath);
        var fileId = ExtractFileIdFromFileName(fileName.AsSpan());
        if (string.IsNullOrEmpty(fileId))
            throw new InvalidOperationException(Constants.Exception.FileIdNotFound);

        var decryptedFileName = RemoveFileIdFromFileName(fileName.AsSpan());
        var decryptedFilePath = Path.Combine(BaseDirectory, Constants.Vault.DecryptedFolder, decryptedFileName);

        await storageService.DecryptFileAsync(
            new FileProcessingRequest(fileId, sourcePath, decryptedFilePath), key, token);

        if (File.Exists(decryptedFilePath))
        {
            await using var fileStream = CreateFileStream(decryptedFilePath, FileMode.Open, FileAccess.Write, null);
            await fileStream.FlushAsync(token);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FileStream CreateFileStream(
        string path,
        FileMode mode,
        FileAccess access,
        ILogger<StorageService>? logger)
    {
        var share = access is FileAccess.Read ? FileShare.Read : FileShare.None;
        var options = FileOptions.Asynchronous
                      | FileOptions.SequentialScan
                      | (access is FileAccess.Write ? FileOptions.WriteThrough : FileOptions.None);

        return DirectStreamFactory.Create(
            path,
            mode,
            access,
            share,
            SafeObject.Core.Helpers.Constants.Storage.BufferSize,
            options,
            logger);
    }

    private static bool IsFileEncrypted(ReadOnlySpan<char> fileName)
    {
        var lastUnderscoreIndex = fileName.LastIndexOf('_');
        if (lastUnderscoreIndex is -1 || lastUnderscoreIndex >= fileName.Length - 1)
            return false;

        var candidateGuid = fileName[(lastUnderscoreIndex + 1)..];
        return Guid.TryParse(candidateGuid, out _);
    }

    private static string ExtractFileIdFromFileName(ReadOnlySpan<char> fileName)
    {
        var lastUnderscoreIndex = fileName.LastIndexOf('_');
        if (lastUnderscoreIndex <= 0 || lastUnderscoreIndex >= fileName.Length - 1)
            return string.Empty;

        return fileName[(lastUnderscoreIndex + 1)..].ToString();
    }

    private static string RemoveFileIdFromFileName(ReadOnlySpan<char> fileName)
    {
        var lastUnderscoreIndex = fileName.LastIndexOf('_');
        return lastUnderscoreIndex <= 0 ? fileName.ToString() : fileName[..lastUnderscoreIndex].ToString();
    }
}