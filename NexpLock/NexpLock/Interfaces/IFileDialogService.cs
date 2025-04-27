using System.Threading.Tasks;
using Avalonia.Controls;

namespace NexpLock.Interfaces;

public interface IFileDialogService
{
    Task<string?> OpenFileAsync(Window window);
}