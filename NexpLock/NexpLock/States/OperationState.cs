using ReactiveUI;

namespace NexpLock.States;

public sealed class OperationState : ReactiveObject
{
    public string? SourceFilePath
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? EncryptionKey
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string OperationStatus
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = Constants.Operation.Ready;

    public int ProgressValue
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsProgressVisible
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
}