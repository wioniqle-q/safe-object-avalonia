using NexpLock.Interfaces;
using NexpLock.States;

namespace NexpLock.Subscriptions;

public sealed class EncryptionOperationSubscriptions(
    IEncryptionOperationRunner encryptionOperationRunner,
    OperationState state) : IEncryptionOperationSubscriptions
{
    public void Subscribe()
    {
        encryptionOperationRunner.ProgressUpdated += OnProgressUpdated;
        encryptionOperationRunner.OperationCompleted += OnOperationCompleted;
    }

    public void Unsubscribe()
    {
        encryptionOperationRunner.ProgressUpdated -= OnProgressUpdated;
        encryptionOperationRunner.OperationCompleted -= OnOperationCompleted;
    }

    private void OnProgressUpdated(int value, string message)
    {
        state.ProgressValue = value;
        state.OperationStatus = message;
    }

    private void OnOperationCompleted(string status)
    {
        state.OperationStatus = status;
        state.IsProgressVisible = false;
    }
}