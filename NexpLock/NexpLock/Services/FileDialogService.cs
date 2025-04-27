using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using NexpLock.Interfaces;

namespace NexpLock.Services;

public sealed class FileDialogService : IFileDialogService
{
    public async Task<string?> OpenFileAsync(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (window.StorageProvider is null)
            throw new InvalidOperationException(Constants.Exception.NoStorageProvider);

        var suggestedStartLocation =
            Path.Combine(Environment.CurrentDirectory, Constants.Crypto.SuggestedStartLocation);

        var options = new FilePickerOpenOptions
        {
            Title = Constants.FileDialog.OpenFileTitle,
            AllowMultiple = false,
            SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(suggestedStartLocation)
        };

        var result = await window.StorageProvider.OpenFilePickerAsync(options);
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }
}