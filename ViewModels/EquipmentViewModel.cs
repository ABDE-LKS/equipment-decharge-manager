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
    public string SerialNumber => string.IsNullOrWhiteSpace(Equipment.SerialNumber) ? "—" : Equipment.SerialNumber.Trim();
    public string InventoryNumber => string.IsNullOrWhiteSpace(Equipment.InventoryNumber) ? "—" : Equipment.InventoryNumber.Trim();
    public string ShCode => string.IsNullOrWhiteSpace(Equipment.ShCode) ? "—" : Equipment.ShCode.Trim();
    public EquipmentStatus Status => Equipment.Status;

    public string DisplayTitle => $"{Type} {Brand} {Model}".Trim();
    public string StatusText => Status == EquipmentStatus.Assigned ? "Assigné" : "Disponible";
    public string StatusBgColor => Status == EquipmentStatus.Assigned ? "#EFF6FF" : "#ECFDF5";
    public string StatusFgColor => Status == EquipmentStatus.Assigned ? "#3B82F6" : "#10B981";
}

public partial class EquipmentViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<EquipmentItemViewModel> _equipments = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private EquipmentStatus? _selectedStatusFilter;

    public List<EquipmentStatus?> StatusFilterOptions { get; } = new()
    {
        null,
        EquipmentStatus.Available,
        EquipmentStatus.Assigned
    };

    public List<EquipmentStatus> EquipmentStatusOptions { get; } = new()
    {
        EquipmentStatus.Available,
        EquipmentStatus.Assigned
    };

    // Form Properties (Add / Edit)
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

    // Delete Confirmation Modal Properties
    [ObservableProperty]
    private bool _isDeleteConfirmOpen;

    [ObservableProperty]
    private EquipmentItemViewModel? _equipmentToDelete;

    [ObservableProperty]
    private bool _isAssignedToActiveDecharge;

    [ObservableProperty]
    private bool _isReferencedInHistory;

    [ObservableProperty]
    private int _historyDechargeCount;

    [ObservableProperty]
    private bool _canDeleteEquipment;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _successMessage = string.Empty;

    public EquipmentViewModel(string? initialSearch = null)
    {
        if (!string.IsNullOrWhiteSpace(initialSearch))
        {
            SearchText = initialSearch;
        }

        _ = LoadEquipmentAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadEquipmentAsync();
    partial void OnSelectedStatusFilterChanged(EquipmentStatus? value) => _ = LoadEquipmentAsync();

    [RelayCommand]
    public async Task OpenDeleteConfirmationAsync(EquipmentItemViewModel item)
    {
        if (item?.Equipment == null) return;

        EquipmentToDelete = item;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            using var db = DatabaseInitializer.CreateDbContext();

            // Check if equipment is assigned to an ACTIVE décharge
            int activeDechargeCount = await db.DechargeItems
                .CountAsync(di => di.EquipmentId == item.Id && di.Decharge.Status == "ACTIVE");

            IsAssignedToActiveDecharge = activeDechargeCount > 0 || item.Equipment.Status == EquipmentStatus.Assigned;
            CanDeleteEquipment = !IsAssignedToActiveDecharge;

            IsDeleteConfirmOpen = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = "Impossible de vérifier le statut de l'équipement : " + ex.Message;
        }
    }

    [RelayCommand]
    public void CancelDeleteConfirmation()
    {
        IsDeleteConfirmOpen = false;
        EquipmentToDelete = null;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    public async Task ConfirmDeleteEquipmentAsync()
    {
        if (EquipmentToDelete == null || !CanDeleteEquipment) return;

        try
        {
            using var db = DatabaseInitializer.CreateDbContext();
            var eq = await db.Equipments.FindAsync(EquipmentToDelete.Id);
            if (eq != null)
            {
                string title = $"{eq.Type} {eq.Brand} {eq.Model}".Trim();
                db.Equipments.Remove(eq);
                await db.SaveChangesAsync();

                IsDeleteConfirmOpen = false;
                EquipmentToDelete = null;
                SuccessMessage = $"✓ Équipement '{title}' supprimé avec succès";

                await LoadEquipmentAsync();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Erreur lors de la suppression : " + ex.Message;
        }
    }

    [RelayCommand]
    public async Task LoadEquipmentAsync()
    {
        try
        {
            using var db = DatabaseInitializer.CreateDbContext();
            var query = db.Equipments.AsQueryable();

            if (SelectedStatusFilter.HasValue)
            {
                if (SelectedStatusFilter.Value == EquipmentStatus.Available)
                {
                    query = query.Where(e => e.Status != EquipmentStatus.Assigned);
                }
                else
                {
                    query = query.Where(e => e.Status == EquipmentStatus.Assigned);
                }
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string term = SearchText.Trim().ToLower();
                query = query.Where(e =>
                    e.Type.ToLower().Contains(term) ||
                    e.Brand.ToLower().Contains(term) ||
                    e.Model.ToLower().Contains(term) ||
                    (e.SerialNumber ?? string.Empty).ToLower().Contains(term) ||
                    (e.InventoryNumber ?? string.Empty).ToLower().Contains(term) ||
                    (e.ShCode ?? string.Empty).ToLower().Contains(term));
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
        FormTitle = "Ajouter un équipement";
        Type = string.Empty;
        Brand = string.Empty;
        Model = string.Empty;
        SerialNumber = string.Empty;
        InventoryNumber = string.Empty;
        ShCode = string.Empty;
        Status = EquipmentStatus.Available;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand]
    public void OpenEditForm(EquipmentItemViewModel item)
    {
        if (item?.Equipment == null) return;
        var eq = item.Equipment;
        EditingEquipmentId = eq.Id;
        FormTitle = "Modifier l'équipement";
        Type = eq.Type;
        Brand = eq.Brand;
        Model = eq.Model;
        SerialNumber = eq.SerialNumber ?? string.Empty;
        InventoryNumber = eq.InventoryNumber ?? string.Empty;
        ShCode = eq.ShCode ?? string.Empty;
        Status = eq.Status == EquipmentStatus.Assigned ? EquipmentStatus.Assigned : EquipmentStatus.Available;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
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
            string.IsNullOrWhiteSpace(Model))
        {
            ErrorMessage = "Champ obligatoire";
            return;
        }

        try
        {
            using var db = DatabaseInitializer.CreateDbContext();

            var trimmedSerial = string.IsNullOrWhiteSpace(SerialNumber) ? null : SerialNumber.Trim();
            var trimmedInventory = string.IsNullOrWhiteSpace(InventoryNumber) ? null : InventoryNumber.Trim();
            var trimmedShCode = string.IsNullOrWhiteSpace(ShCode) ? null : ShCode.Trim();

            // Validate unique serial number only when a value exists
            if (!string.IsNullOrWhiteSpace(trimmedSerial))
            {
                var existingSerial = await db.Equipments
                    .FirstOrDefaultAsync(e => e.SerialNumber != null && e.SerialNumber.ToLower() == trimmedSerial.ToLower() &&
                                             (EditingEquipmentId == null || e.Id != EditingEquipmentId.Value));
                if (existingSerial != null)
                {
                    ErrorMessage = "Ce numéro de série existe déjà.";
                    return;
                }
            }

            // Validate unique inventory number only when a value exists
            if (!string.IsNullOrWhiteSpace(trimmedInventory))
            {
                var existingInv = await db.Equipments
                    .FirstOrDefaultAsync(e => e.InventoryNumber != null && e.InventoryNumber.ToLower() == trimmedInventory.ToLower() &&
                                             (EditingEquipmentId == null || e.Id != EditingEquipmentId.Value));
                if (existingInv != null)
                {
                    ErrorMessage = "Ce numéro d'inventaire existe déjà.";
                    return;
                }
            }

            if (EditingEquipmentId.HasValue)
            {
                var eq = await db.Equipments.FindAsync(EditingEquipmentId.Value);
                if (eq != null)
                {
                    eq.Type = Type.Trim();
                    eq.Brand = Brand.Trim();
                    eq.Model = Model.Trim();
                    eq.SerialNumber = trimmedSerial;
                    eq.InventoryNumber = trimmedInventory;
                    eq.ShCode = trimmedShCode;
                    eq.Status = Status;
                }
            }
            else
            {
                var equipment = new Equipment
                {
                    Type = Type.Trim(),
                    Brand = Brand.Trim(),
                    Model = Model.Trim(),
                    SerialNumber = trimmedSerial,
                    InventoryNumber = trimmedInventory,
                    ShCode = trimmedShCode,
                    Status = Status
                };

                db.Equipments.Add(equipment);
            }

            await db.SaveChangesAsync();
            IsFormOpen = false;
            SuccessMessage = $"✓ Équipement '{Type.Trim()} {Brand.Trim()}' enregistré avec succès";
            await LoadEquipmentAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
