using System;
using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;

namespace NexpLock.Interfaces;

public interface INexpWindowCommandManager
{
    string KeyTextBoxPasswordChar { get; }
    ReactiveCommand<Window, Unit>? CreateMinimizeCommand();
    ReactiveCommand<Window, Unit>? CreateCloseCommand();
    ReactiveCommand<Window, Unit>? CreateBrowseCommand();
    ReactiveCommand<Unit, Unit>? CreateGenerateKeyCommand();
    ReactiveCommand<Unit, Unit>? CreateTogglePasswordVisibilityCommand(Action onPasswordCharChanged);
    ReactiveCommand<Unit, Unit>? CreateEncryptCommand(Func<IObservable<bool>> canExecute);
    ReactiveCommand<Unit, Unit>? CreateDecryptCommand(Func<IObservable<bool>> canExecute);
    ReactiveCommand<Unit, Unit>? CreateCancelOperationCommand();
}