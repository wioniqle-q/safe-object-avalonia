using System;
using Avalonia.Interactivity;
using NexpLock.ViewModels;

namespace NexpLock.Views;

public sealed partial class NexpWindow
{
    private void OnGenerateKeyButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not NexpWindowViewModel vm)
            return;
        vm.GenerateKeyCommand!.Execute().Subscribe();
    }

    private void OnTogglePasswordButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not NexpWindowViewModel vm)
            return;
        vm.TogglePasswordVisibilityCommand!.Execute().Subscribe();
    }

    private void OnEncryptButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not NexpWindowViewModel vm)
            return;
        vm.EncryptCommand!.Execute().Subscribe();
    }

    private void OnDecryptButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not NexpWindowViewModel vm)
            return;
        vm.DecryptCommand!.Execute().Subscribe();
    }

    private void OnCancelButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not NexpWindowViewModel vm)
            return;
        vm.CancelOperationCommand!.Execute().Subscribe();
    }
}