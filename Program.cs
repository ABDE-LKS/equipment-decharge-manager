using Avalonia;
using System;
using System.Threading;

namespace EquipmentDechargeManager;

sealed class Program
{
    private const string SingleInstanceMutexName = "Global\\EquipmentDechargeManager_SingleInstance";

    [STAThread]
    public static void Main(string[] args)
    {
        using var mutex = new Mutex(true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
            return;

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
