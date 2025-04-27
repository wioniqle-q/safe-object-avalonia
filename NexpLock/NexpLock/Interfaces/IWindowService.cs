using Avalonia.Controls;

namespace NexpLock.Interfaces;

public interface IWindowService
{
    void Minimize(Window window);
    void Close(Window window);
}