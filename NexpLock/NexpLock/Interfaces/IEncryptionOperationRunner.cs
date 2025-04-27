using System;
using System.Threading;
using System.Threading.Tasks;

namespace NexpLock.Interfaces;

public interface IEncryptionOperationRunner
{
    internal event Action<int, string> ProgressUpdated;
    internal event Action<string> OperationCompleted;

    Task RunEncryptOperationAsync(string filePath, string encryptionKey, CancellationToken cancellationToken);
    Task RunDecryptOperationAsync(string filePath, string encryptionKey, CancellationToken cancellationToken);
}