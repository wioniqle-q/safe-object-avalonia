using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using NexpLock.Interfaces;
using NexpLock.States;
using ReactiveUI;

namespace NexpLock.Commands;

public sealed class NexpWindowCommandManager(
    IWindowService windowService,
    IFileDialogService fileDialogService,
    IKeyService keyService,
    IEncryptionOperationRunner encryptionOperationRunner,
    OperationState state)
    : INexpWindowCommandManager
{
    private CancellationTokenSource? _cts;
    private volatile bool _isKeyHidden = true;

    public ReactiveCommand<Window, Unit> CreateMinimizeCommand()
    {
        return ReactiveCommand.Create<Window>(windowService.Minimize);
    }

    public ReactiveCommand<Window, Unit> CreateCloseCommand()
    {
        return ReactiveCommand.Create<Window>(windowService.Close);
    }

    public ReactiveCommand<Window, Unit> CreateBrowseCommand()
    {
        return ReactiveCommand.CreateFromTask<Window>(async window =>
        {
            if (state.IsProgressVisible)
                return;

            var file = await fileDialogService.OpenFileAsync(window);
            if (string.IsNullOrEmpty(file) is not true)
                state.SourceFilePath = file;
        });
    }

    public ReactiveCommand<Unit, Unit> CreateGenerateKeyCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (state.IsProgressVisible)
                return;

            var key = await Task.Run(keyService.GenerateKey);

            if (string.IsNullOrEmpty(key) is not true)
                state.EncryptionKey = key;
        });
    }

    public ReactiveCommand<Unit, Unit> CreateEncryptCommand(Func<IObservable<bool>> canExecute)
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (string.IsNullOrEmpty(state.SourceFilePath) || string.IsNullOrEmpty(state.EncryptionKey))
                return;

            if (state.IsProgressVisible)
                return;

            state.IsProgressVisible = true;
            _cts = new CancellationTokenSource();

            await encryptionOperationRunner.RunEncryptOperationAsync(state.SourceFilePath, state.EncryptionKey,
                _cts.Token);
        }, canExecute());
    }

    public ReactiveCommand<Unit, Unit> CreateDecryptCommand(Func<IObservable<bool>> canExecute)
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (string.IsNullOrEmpty(state.SourceFilePath) || string.IsNullOrEmpty(state.EncryptionKey))
                return;

            if (state.IsProgressVisible)
                return;

            state.IsProgressVisible = true;
            _cts = new CancellationTokenSource();

            await encryptionOperationRunner.RunDecryptOperationAsync(state.SourceFilePath, state.EncryptionKey,
                _cts.Token);
        }, canExecute());
    }

    public ReactiveCommand<Unit, Unit> CreateCancelOperationCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            _cts?.CancelAsync();

            state.OperationStatus = Constants.Operation.Canceled;
        });
    }

    public ReactiveCommand<Unit, Unit> CreateTogglePasswordVisibilityCommand(Action raiseKeyPropertyChanged)
    {
        return ReactiveCommand.Create(() =>
        {
            _isKeyHidden = _isKeyHidden is not true;
            raiseKeyPropertyChanged.Invoke();
        });
    }

    public string KeyTextBoxPasswordChar => _isKeyHidden ? "•" : "";
}