using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using EquipmentDechargeManager.Services;
using EquipmentDechargeManager.ViewModels;
using EquipmentDechargeManager.Views;
using System.Threading.Tasks;

namespace EquipmentDechargeManager;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var initResult = await Task.Run(DatabaseInitializer.InitializeAsync);
            if (!initResult.Success)
            {
                await ShowDatabaseErrorAsync(desktop, initResult);
                desktop.Shutdown();
                return;
            }

            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task ShowDatabaseErrorAsync(
        IClassicDesktopStyleApplicationLifetime desktop,
        DatabaseInitResult initResult)
    {
        var message = initResult.ErrorMessage ?? "Database initialization failed.";
        if (!string.IsNullOrWhiteSpace(initResult.SetupInstructions))
        {
            message += $"\n\n{initResult.SetupInstructions}";
        }

        var dialog = new Window
        {
            Title = "Database Setup Required",
            Width = 640,
            Height = 300,
            MaxHeight = 400,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            CanResize = false,
            Topmost = true,
            Content = new ScrollViewer
            {
                Padding = new Thickness(16),
                Content = new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                    FontSize = 13
                }
            }
        };

        if (desktop.MainWindow != null)
        {
            await dialog.ShowDialog(desktop.MainWindow);
            return;
        }

        var dialogClosed = new TaskCompletionSource();
        dialog.Closed += (_, _) => dialogClosed.TrySetResult();
        dialog.Show();
        await dialogClosed.Task;
    }
}
