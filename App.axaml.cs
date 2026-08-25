using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentDechargeManager.Services;
using EquipmentDechargeManager.ViewModels;
using EquipmentDechargeManager.Views;

namespace EquipmentDechargeManager;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        LocalizationManager.Instance.SetCulture("fr");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();

        // Run EF Core migration/schema initialization in background
        await System.Threading.Tasks.Task.Run(DatabaseInitializer.InitializeAsync);
    }
}