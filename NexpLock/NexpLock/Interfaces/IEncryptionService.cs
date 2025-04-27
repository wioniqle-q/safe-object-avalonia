using System;
using System.Threading;
using System.Threading.Tasks;

namespace NexpLock.Interfaces;

public interface IEncryptionService
{
    Task EncryptFileAsync(string filePath, string key, IProgress<(double percentage, string message)> progress,
        CancellationToken token);

    Task DecryptFileAsync(string filePath, string key, IProgress<(double percentage, string message)> progress,
        CancellationToken token);
}