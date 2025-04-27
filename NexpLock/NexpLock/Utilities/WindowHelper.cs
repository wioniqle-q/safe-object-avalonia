using System;
using Avalonia.Controls;

namespace NexpLock.Utilities;

public static class WindowHelper
{
    public static void AdjustWindowSize(Window window)
    {
        if (window is null)
            throw new ArgumentNullException(nameof(window), Constants.Exception.NoWindowInstance);

        if (window.Screens is null)
            throw new InvalidOperationException(Constants.Exception.NoScreensAvailable);

        var primaryScreen = window.Screens.Primary;
        if (primaryScreen is null)
            return;

        var screenBounds = primaryScreen.Bounds;

        var targetWidth = Math.Max(600, screenBounds.Width * 0.5);
        var targetHeight = Math.Max(500, screenBounds.Height * 0.5);

        targetWidth = Math.Min(targetWidth, 1600);
        targetHeight = Math.Min(targetHeight, 1000);

        window.Width = targetWidth;
        window.Height = targetHeight;
    }
}