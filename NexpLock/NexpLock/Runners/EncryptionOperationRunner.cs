using System;
using System.Threading;
using System.Threading.Tasks;
using NexpLock.Interfaces;

namespace NexpLock.Runners;

public sealed class EncryptionOperationRunner(IEncryptionService encryptionService) : IEncryptionOperationRunner
{
    public event Action<int, string> ProgressUpdated = delegate { };
    public event Action<string> OperationCompleted = delegate { };

    public async Task RunEncryptOperationAsync(string filePath, string encryptionKey,
        CancellationToken cancellationToken)
    {
        var progress = new Progress<(double percentage, string message)>(data =>
        {
            ProgressUpdated.Invoke((int)data.percentage, data.message);
        });

        try
        {
            await encryptionService.EncryptFileAsync(filePath, encryptionKey, progress, cancellationToken);
            OperationCompleted.Invoke(Constants.Operation.Completed);
        }
        catch (OperationCanceledException)
        {
            OperationCompleted.Invoke(Constants.Operation.Canceled);
        }
        catch (Exception exception)
        {
            OperationCompleted.Invoke($"{Constants.Operation.Failed}: {exception.Message}");
        }
    }

    public async Task RunDecryptOperationAsync(string filePath, string encryptionKey,
        CancellationToken cancellationToken)
    {
        var progress = new Progress<(double percentage, string message)>(data =>
        {
            ProgressUpdated.Invoke((int)data.percentage, data.message);
        });

        try
        {
            await encryptionService.DecryptFileAsync(filePath, encryptionKey, progress, cancellationToken);
            OperationCompleted.Invoke(Constants.Operation.Completed);
        }
        catch (OperationCanceledException)
        {
            OperationCompleted.Invoke(Constants.Operation.Canceled);
        }
        catch (Exception exception)
        {
            OperationCompleted.Invoke($"{Constants.Operation.Failed}: {exception.Message}");
        }
    }
}