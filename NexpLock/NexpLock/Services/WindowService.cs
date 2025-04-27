using Avalonia.Controls;
using NexpLock.Interfaces;

namespace NexpLock.Services;

public sealed class WindowService : IWindowService
{
    public void Minimize(Window window)
    {
        window.WindowState = WindowState.Minimized;
    }

    public void Close(Window window)
    {
        window.Close();
    }
}