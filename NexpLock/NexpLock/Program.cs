using System;
using System.Threading;
using Avalonia;
using Avalonia.ReactiveUI;

namespace NexpLock;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using var mutex = new Mutex(true, "Global\\NexpLockAppMutex", out var createdNew);
        if (createdNew is not true)
            return;

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .UseReactiveUI();
    }
}