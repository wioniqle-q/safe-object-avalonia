using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SafeObject.Core.Services.Factory;
using static NexpLock.Constants;

namespace NexpLock.Utilities;

public static class DirectoryInitializer
{
    private static readonly string SBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

    private static readonly string[] SRequiredPaths =
    [
        Vault.EncryptedFolder,
        Vault.DecryptedFolder
    ];

    public static async Task EnsureRequiredDirectoriesExist()
    {
        var baseDirSpan = SBaseDirectory.AsSpan();
        var baseLength = baseDirSpan.Length;
        var hasSeparator =
            EqualityComparer<char>.Default.Equals(baseDirSpan[baseLength - 1], Path.DirectorySeparatorChar);
        var totalBaseLength = hasSeparator ? baseLength : baseLength + 1;

        foreach (var t in SRequiredPaths)
        {
            var relSpan = t.AsSpan();
            var finalPath = string.Create(totalBaseLength + relSpan.Length,
                (SBaseDirectory, t, totalBaseLength), (destination, state) =>
                {
                    var (baseDir, relativePath, tBase) = state;
                    baseDir.AsSpan().CopyTo(destination);
                    if (tBase > baseDir.Length) destination[baseDir.Length] = Path.DirectorySeparatorChar;
                    relativePath.AsSpan().CopyTo(destination[tBase..]);
                });

            if (Directory.Exists(finalPath)) continue;

            Directory.CreateDirectory(finalPath);

            var flushFilePath = Path.Combine(finalPath, $"flush_{Guid.CreateVersion7():N}.tmp");

            await using (var stream = DirectStreamFactory.Create(
                             flushFilePath,
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             Encryption.BufferSize,
                             FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.WriteThrough))
            {
                await stream.FlushAsync();
            }

            File.Delete(flushFilePath);
        }
    }
}