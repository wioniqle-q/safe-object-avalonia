using System;
using Avalonia.Interactivity;
using NexpLock.ViewModels;

namespace NexpLock.Views;

public sealed partial class NexpWindow
{
    private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not NexpWindowViewModel vm)
            return;
        vm.MinimizeCommand!.Execute(this).Subscribe();
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not NexpWindowViewModel vm)
            return;
        vm.CloseCommand!.Execute(this).Subscribe();
    }

    private void OnBrowseButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not NexpWindowViewModel vm)
            return;
        vm.BrowseCommand!.Execute(this).Subscribe();
    }
}