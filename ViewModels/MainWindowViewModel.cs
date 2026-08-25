using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentDechargeManager.Services;

namespace EquipmentDechargeManager.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentView;

    [ObservableProperty]
    private string _activeTab = "Dashboard";

    [ObservableProperty]
    private string _selectedLanguage = "FR";

    public LocalizationManager Loc => LocalizationManager.Instance;

    public MainWindowViewModel()
    {
        Loc.SetCulture("fr");
        var dashboardVm = new DashboardViewModel();
        dashboardVm.NavigateToDetailsRequested = NavigateToDechargeDetails;
        _currentView = dashboardVm;
        _activeTab = "Dashboard";
        _selectedLanguage = "FR";
    }

    [RelayCommand]
    public void NavigateDashboard()
    {
        var dashboardVm = new DashboardViewModel();
        dashboardVm.NavigateToDetailsRequested = NavigateToDechargeDetails;
        CurrentView = dashboardVm;
        ActiveTab = "Dashboard";
    }

    [RelayCommand]
    public void NavigateEmployees()
    {
        CurrentView = new EmployeesViewModel();
        ActiveTab = "Employees";
    }

    [RelayCommand]
    public void NavigateEquipment()
    {
        CurrentView = new EquipmentViewModel();
        ActiveTab = "Equipment";
    }

    [RelayCommand]
    public void NavigateDecharges()
    {
        var dechargesVm = new DechargesViewModel();
        dechargesVm.NavigateToDetailsRequested = NavigateToDechargeDetails;
        CurrentView = dechargesVm;
        ActiveTab = "Decharges";
    }

    public void NavigateToDechargeDetails(int dechargeId)
    {
        var detailsVm = new DechargeDetailsViewModel(dechargeId);
        detailsVm.BackRequested = NavigateDecharges;
        CurrentView = detailsVm;
        ActiveTab = "DechargeDetails";
    }

    [RelayCommand]
    public void NavigateSettings()
    {
        CurrentView = new SettingsViewModel();
        ActiveTab = "Settings";
    }

    [RelayCommand]
    public void SetFrench()
    {
        SelectedLanguage = "FR";
        Loc.SetCulture("fr");
    }

    [RelayCommand]
    public void SetEnglish()
    {
        SelectedLanguage = "EN";
        Loc.SetCulture("en");
    }
}
