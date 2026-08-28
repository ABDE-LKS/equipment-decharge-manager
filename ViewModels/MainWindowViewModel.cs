using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EquipmentDechargeManager.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentView;

    [ObservableProperty]
    private string _activeTab = "Dashboard";

    public bool IsDashboardActive => ActiveTab == "Dashboard";
    public bool IsEmployeesActive => ActiveTab == "Employees";
    public bool IsEquipmentActive => ActiveTab == "Equipment";
    public bool IsHistoryActive => ActiveTab == "History";
    public bool IsDechargesActive => ActiveTab == "Decharges";
    public bool IsDechargeDetailsActive => ActiveTab == "DechargeDetails";
    public bool IsSettingsActive => ActiveTab == "Settings";

    partial void OnActiveTabChanged(string value)
    {
        OnPropertyChanged(nameof(IsDashboardActive));
        OnPropertyChanged(nameof(IsEmployeesActive));
        OnPropertyChanged(nameof(IsEquipmentActive));
        OnPropertyChanged(nameof(IsHistoryActive));
        OnPropertyChanged(nameof(IsDechargesActive));
        OnPropertyChanged(nameof(IsDechargeDetailsActive));
        OnPropertyChanged(nameof(IsSettingsActive));
    }



    public MainWindowViewModel()
    {
        var dashboardVm = new DashboardViewModel();
        dashboardVm.NavigateToDetailsRequested = (id) => NavigateToDechargeDetails(id, false);
        _currentView = dashboardVm;
        _activeTab = "Dashboard";
    }

    [RelayCommand]
    public void NavigateDashboard()
    {
        var dashboardVm = new DashboardViewModel();
        dashboardVm.NavigateToDetailsRequested = (id) => NavigateToDechargeDetails(id, false);
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
    public void NavigateHistory()
    {
        var historyVm = new HistoryViewModel();
        historyVm.NavigateToDetailsRequested = (id) => NavigateToDechargeDetails(id, false);
        CurrentView = historyVm;
        ActiveTab = "History";
    }

    [RelayCommand]
    public void NavigateDecharges()
    {
        var dechargesVm = new DechargesViewModel();
        dechargesVm.NavigateToDetailsRequested = (id, openReturnModal) => NavigateToDechargeDetails(id, openReturnModal);
        CurrentView = dechargesVm;
        ActiveTab = "Decharges";
    }

    public void NavigateToDechargeDetails(int dechargeId, bool openReturnModal = false)
    {
        var detailsVm = new DechargeDetailsViewModel(dechargeId, openReturnModal);
        detailsVm.BackRequested = NavigateDecharges;
        detailsVm.ViewEmployeeRequested = NavigateToEmployee;
        detailsVm.ViewEquipmentRequested = NavigateToEquipment;
        CurrentView = detailsVm;
        ActiveTab = "DechargeDetails";
    }

    public void NavigateToEmployee(int employeeId, string matricule)
    {
        var vm = new EmployeesViewModel(matricule);
        CurrentView = vm;
        ActiveTab = "Employees";
    }

    public void NavigateToEquipment(int equipmentId, string inventoryNumber)
    {
        var vm = new EquipmentViewModel(inventoryNumber);
        CurrentView = vm;
        ActiveTab = "Equipment";
    }

    [RelayCommand]
    public void NavigateSettings()
    {
        CurrentView = new SettingsViewModel();
        ActiveTab = "Settings";
    }
}
