using System;
using System.Reactive;
using Avalonia.Controls;
using NexpLock.Commands;
using NexpLock.Interfaces;
using NexpLock.Runners;
using NexpLock.States;
using NexpLock.Subscriptions;
using ReactiveUI;

namespace NexpLock.ViewModels;

public sealed class NexpWindowViewModel : ViewModelBase
{
    private readonly NexpWindowCommandManager _commandManager;
    private readonly EncryptionOperationRunner _encryptionOperationRunner;
    private EncryptionOperationSubscriptions? _encryptionSubscriptions;

    public NexpWindowViewModel(
        IWindowService windowService,
        IFileDialogService fileDialogService,
        IKeyService keyService,
        IEncryptionOperationRunner encryptionOperationRunner)
    {
        _encryptionOperationRunner = (encryptionOperationRunner as EncryptionOperationRunner)!;

        _commandManager = new NexpWindowCommandManager(windowService, fileDialogService, keyService,
            encryptionOperationRunner, State);

        InitializeCommands();
        InitializeSubscriptions();
    }

    public string KeyTextBoxPasswordChar => _commandManager.KeyTextBoxPasswordChar;

    public OperationState State { get; } = new();

    public ReactiveCommand<Window, Unit>? MinimizeCommand { get; private set; }
    public ReactiveCommand<Window, Unit>? CloseCommand { get; private set; }
    public ReactiveCommand<Window, Unit>? BrowseCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? GenerateKeyCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? TogglePasswordVisibilityCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? EncryptCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? DecryptCommand { get; private set; }
    public ReactiveCommand<Unit, Unit>? CancelOperationCommand { get; private set; }

    private void InitializeCommands()
    {
        MinimizeCommand = _commandManager.CreateMinimizeCommand();
        CloseCommand = _commandManager.CreateCloseCommand();
        BrowseCommand = _commandManager.CreateBrowseCommand();
        GenerateKeyCommand = _commandManager.CreateGenerateKeyCommand();
        TogglePasswordVisibilityCommand =
            _commandManager.CreateTogglePasswordVisibilityCommand(() =>
                this.RaisePropertyChanged(nameof(KeyTextBoxPasswordChar)));
        EncryptCommand = _commandManager.CreateEncryptCommand(CreateCanStartOperationObservable);
        DecryptCommand = _commandManager.CreateDecryptCommand(CreateCanStartOperationObservable);
        CancelOperationCommand = _commandManager.CreateCancelOperationCommand();
    }

    private void InitializeSubscriptions()
    {
        _encryptionSubscriptions = new EncryptionOperationSubscriptions(_encryptionOperationRunner, State);
        _encryptionSubscriptions.Subscribe();
    }

    private IObservable<bool> CreateCanStartOperationObservable()
    {
        return this.WhenAnyValue(
            x => x.State.SourceFilePath,
            x => x.State.EncryptionKey,
            x => x.State.IsProgressVisible,
            (file, key, isInProgress) => string.IsNullOrEmpty(file) is not true &&
                                         string.IsNullOrEmpty(key) is not true &&
                                         isInProgress is not true);
    }
}