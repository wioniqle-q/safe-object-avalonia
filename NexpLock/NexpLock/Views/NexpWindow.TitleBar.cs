using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace NexpLock.Views;

public sealed partial class NexpWindow
{
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        Dispatcher.UIThread.InvokeAsync(() => { Topmost = false; }, DispatcherPriority.Background);
    }

    private void OnTitleBarPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed is not true || e.Source is TextBlock or Button)
            return;

        BeginMoveDrag(e);
    }
}