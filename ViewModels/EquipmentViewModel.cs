using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentDechargeManager.Data.Entities;
using EquipmentDechargeManager.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace EquipmentDechargeManager.ViewModels;

public class EquipmentItemViewModel
{
    public Equipment Equipment { get; set; } = null!;
    public int Id => Equipment.Id;
    public string Type => Equipment.Type;
    public string Brand => Equipment.Brand;
    public string Model => Equipment.Model;
    public string SerialNumber => Equipment.SerialNumber;
    public string InventoryNumber => Equipment.InventoryNumber;
    public EquipmentStatus Status => Equipment.Status;

    public string StatusText => Status switch
    {
        EquipmentStatus.Available => "Disponible",
        EquipmentStatus.Assigned => "Assigné",
        EquipmentStatus.Returned => "Retourné",
        EquipmentStatus.Damaged => "Endommagé",
        EquipmentStatus.Lost => "Perdu",
        EquipmentStatus.Retired => "Retiré",
        _ => Status.ToString()
    };

    public string StatusBgColor => Status switch
    {
        EquipmentStatus.Available => "#ECFDF5",
        EquipmentStatus.Assigned => "#EFF6FF",
        EquipmentStatus.Returned => "#F0FDFA",
        EquipmentStatus.Damaged => "#FFF7ED",
        EquipmentStatus.Lost => "#FEF2F2",
        _ => "#F1F5F9"
    };

    public string StatusFgColor => Status switch
    {
        EquipmentStatus.Available => "#10B981",
        EquipmentStatus.Assigned => "#3B82F6",
        EquipmentStatus.Returned => "#06B6D4",
        EquipmentStatus.Damaged => "#F97316",
        EquipmentStatus.Lost => "#EF4444",
        _ => "#64748B"
    };
}

public partial class EquipmentViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<EquipmentItemViewModel> _equipments = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private EquipmentStatus? _selectedStatusFilter;

    // Available statuses for combo box
    public List<EquipmentStatus?> StatusFilterOptions { get; } = new()
    {
        null,
        EquipmentStatus.Available,
        EquipmentStatus.Assigned,
        EquipmentStatus.Returned,
        EquipmentStatus.Damaged,
        EquipmentStatus.Lost,
        EquipmentStatus.Retired
    };

    public List<EquipmentStatus> EquipmentStatusOptions { get; } = Enum.GetValues<EquipmentStatus>().ToList();

    // Form Properties
    [ObservableProperty]
    private bool _isFormOpen;

    [ObservableProperty]
    private string _formTitle = string.Empty;

    [ObservableProperty]
    private int? _editingEquipmentId;

    [ObservableProperty]
    private string _type = string.Empty;

    [ObservableProperty]
    private string _brand = string.Empty;

    [ObservableProperty]
    private string _model = string.Empty;

    [ObservableProperty]
    private string _serialNumber = string.Empty;

    [ObservableProperty]
    private string _inventoryNumber = string.Empty;

    [ObservableProperty]
    private string _shCode = string.Empty;

    [ObservableProperty]
    private EquipmentStatus _status = EquipmentStatus.Available;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public LocalizationManager Loc => LocalizationManager.Instance;

    public EquipmentViewModel()
    {
        _ = LoadEquipmentAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadEquipmentAsync();
    partial void OnSelectedStatusFilterChanged(EquipmentStatus? value) => _ = LoadEquipmentAsync();

    [RelayCommand]
    public async Task LoadEquipmentAsync()
    {
        try
        {
            using var db = DatabaseInitializer.CreateDbContext();
            var query = db.Equipments.AsQueryable();

            if (SelectedStatusFilter.HasValue)
            {
                query = query.Where(e => e.Status == SelectedStatusFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string term = SearchText.Trim().ToLower();
                query = query.Where(e =>
                    e.Type.ToLower().Contains(term) ||
                    e.Brand.ToLower().Contains(term) ||
                    e.Model.ToLower().Contains(term) ||
                    e.SerialNumber.ToLower().Contains(term) ||
                    e.InventoryNumber.ToLower().Contains(term));
            }

            var list = await query.OrderBy(e => e.Brand).ThenBy(e => e.Model).ToListAsync();
            
            var itemsList = list.Select(e => new EquipmentItemViewModel { Equipment = e }).ToList();

            Equipments = new ObservableCollection<EquipmentItemViewModel>(itemsList);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public void OpenAddForm()
    {
        EditingEquipmentId = null;
        FormTitle = Loc["Eq_AddTitle"];
        Type = string.Empty;
        Brand = string.Empty;
        Model = string.Empty;
        SerialNumber = string.Empty;
        InventoryNumber = string.Empty;
        ShCode = string.Empty;
        Status = EquipmentStatus.Available;
        ErrorMessage = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand]
    public void OpenEditForm(EquipmentItemViewModel item)
    {
        if (item?.Equipment == null) return;
        var eq = item.Equipment;
        EditingEquipmentId = eq.Id;
        FormTitle = Loc["Eq_EditTitle"];
        Type = eq.Type;
        Brand = eq.Brand;
        Model = eq.Model;
        SerialNumber = eq.SerialNumber;
        InventoryNumber = eq.InventoryNumber;
        ShCode = eq.ShCode ?? string.Empty;
        Status = eq.Status;
        ErrorMessage = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand]
    public void CancelForm()
    {
        IsFormOpen = false;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    public async Task SaveFormAsync()
    {
        if (string.IsNullOrWhiteSpace(Type) ||
            string.IsNullOrWhiteSpace(Brand) ||
            string.IsNullOrWhiteSpace(Model) ||
            string.IsNullOrWhiteSpace(SerialNumber) ||
            string.IsNullOrWhiteSpace(InventoryNumber))
        {
            ErrorMessage = Loc["Common_Required"];
            return;
        }

        try
        {
            using var db = DatabaseInitializer.CreateDbContext();

            // Validate unique serial number
            bool serialExists = await db.Equipments.AnyAsync(e =>
                e.SerialNumber.ToLower() == SerialNumber.Trim().ToLower() &&
                (!EditingEquipmentId.HasValue || e.Id != EditingEquipmentId.Value));

            if (serialExists)
            {
                ErrorMessage = Loc["Eq_ErrorSerialExists"];
                return;
            }

            // Validate unique inventory number
            bool inventoryExists = await db.Equipments.AnyAsync(e =>
                e.InventoryNumber.ToLower() == InventoryNumber.Trim().ToLower() &&
                (!EditingEquipmentId.HasValue || e.Id != EditingEquipmentId.Value));

            if (inventoryExists)
            {
                ErrorMessage = Loc["Eq_ErrorInventoryExists"];
                return;
            }

            if (EditingEquipmentId.HasValue)
            {
                var eq = await db.Equipments.FindAsync(EditingEquipmentId.Value);
                if (eq != null)
                {
                    eq.Type = Type.Trim();
                    eq.Brand = Brand.Trim();
                    eq.Model = Model.Trim();
                    eq.SerialNumber = SerialNumber.Trim();
                    eq.InventoryNumber = InventoryNumber.Trim();
                    eq.ShCode = string.IsNullOrWhiteSpace(ShCode) ? null : ShCode.Trim();
                    eq.Status = Status;
                }
            }
            else
            {
                var eq = new Equipment
                {
                    Type = Type.Trim(),
                    Brand = Brand.Trim(),
                    Model = Model.Trim(),
                    SerialNumber = SerialNumber.Trim(),
                    InventoryNumber = InventoryNumber.Trim(),
                    ShCode = string.IsNullOrWhiteSpace(ShCode) ? null : ShCode.Trim(),
                    Status = Status
                };
                db.Equipments.Add(eq);
            }

            await db.SaveChangesAsync();
            IsFormOpen = false;
            await LoadEquipmentAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public async Task DeleteEquipmentAsync(EquipmentItemViewModel item)
    {
        if (item?.Equipment == null) return;
        try
        {
            using var db = DatabaseInitializer.CreateDbContext();
            var eq = await db.Equipments.FindAsync(item.Equipment.Id);
            if (eq != null)
            {
                db.Equipments.Remove(eq);
                await db.SaveChangesAsync();
                await LoadEquipmentAsync();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
