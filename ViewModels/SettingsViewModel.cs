using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentDechargeManager.Services;

namespace EquipmentDechargeManager.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _activeSection = "Employees";

    [ObservableProperty]
    private DataImportWizardViewModel _activeWizard;

    public DataImportWizardViewModel EmployeeWizard { get; }
    public DataImportWizardViewModel EquipmentWizard { get; }

    public LocalizationManager Loc => LocalizationManager.Instance;

    public SettingsViewModel()
    {
        EmployeeWizard = new DataImportWizardViewModel(ImportEntityType.Employee);
        EquipmentWizard = new DataImportWizardViewModel(ImportEntityType.Equipment);

        _activeWizard = EmployeeWizard;
        _activeSection = "Employees";
    }

    [RelayCommand]
    public void SelectEmployeesImport()
    {
        ActiveSection = "Employees";
        ActiveWizard = EmployeeWizard;
    }

    [RelayCommand]
    public void SelectEquipmentImport()
    {
        ActiveSection = "Equipment";
        ActiveWizard = EquipmentWizard;
    }
}
