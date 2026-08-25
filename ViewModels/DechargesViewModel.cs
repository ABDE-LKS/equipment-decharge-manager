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

public class DechargeSummaryModel
{
    public int Id { get; set; }
    public string DechargeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public int ItemCount { get; set; }
    public int ReturnedItemCount { get; set; }
    
    public string StatusText
    {
        get
        {
            if (ReturnedItemCount == 0) return "Active";
            if (ReturnedItemCount < ItemCount) return "Partiellement retournée";
            return "Retournée";
        }
    }

    public string StatusBgColor => StatusText switch
    {
        "Active" => "#FEF3C7",
        "Partiellement retournée" => "#FFEDD5",
        "Retournée" => "#D1FAE5",
        _ => "#FEF3C7"
    };

    public string StatusFgColor => StatusText switch
    {
        "Active" => "#D97706",
        "Partiellement retournée" => "#C2410C",
        "Retournée" => "#059669",
        _ => "#D97706"
    };

    public bool IsCompleted => ReturnedItemCount == ItemCount;
}

public partial class NewDechargeItemModel : ObservableObject
{
    public Equipment Equipment { get; set; } = null!;

    [ObservableProperty]
    private string _conditionAtAssignment = "Neuf / Good";
}

public partial class DechargesViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<DechargeSummaryModel> _decharges = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    // Create Form Properties
    [ObservableProperty]
    private bool _isCreateFormOpen;

    [ObservableProperty]
    private string _dechargeNumber = string.Empty;

    [ObservableProperty]
    private Employee? _selectedEmployee;

    [ObservableProperty]
    private ObservableCollection<Employee> _employeesList = new();

    [ObservableProperty]
    private ObservableCollection<Equipment> _availableEquipmentsList = new();

    [ObservableProperty]
    private Equipment? _selectedEquipmentToAdd;

    [ObservableProperty]
    private string _itemCondition = "Neuf";

    [ObservableProperty]
    private ObservableCollection<NewDechargeItemModel> _newDechargeItems = new();

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public Action<int>? NavigateToDetailsRequested { get; set; }

    public LocalizationManager Loc => LocalizationManager.Instance;

    public DechargesViewModel()
    {
        _ = LoadDechargesAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadDechargesAsync();

    [RelayCommand]
    public async Task LoadDechargesAsync()
    {
        try
        {
            using var db = DatabaseInitializer.CreateDbContext();
            var query = db.Decharges
                .Include(d => d.Employee)
                .Include(d => d.Items)
                    .ThenInclude(i => i.ReturnRecord)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string term = SearchText.Trim().ToLower();
                query = query.Where(d =>
                    d.DechargeNumber.ToLower().Contains(term) ||
                    d.Employee.FullName.ToLower().Contains(term) ||
                    d.Employee.Matricule.ToLower().Contains(term));
            }

            var list = await query.OrderByDescending(d => d.IssueDate).ToListAsync();

            var summaryList = list.Select(d => new DechargeSummaryModel
            {
                Id = d.Id,
                DechargeNumber = d.DechargeNumber,
                EmployeeName = d.Employee.FullName,
                IssueDate = d.IssueDate,
                ItemCount = d.Items.Count,
                ReturnedItemCount = d.Items.Count(i => i.ReturnRecord != null)
            }).ToList();

            Decharges = new ObservableCollection<DechargeSummaryModel>(summaryList);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public async Task OpenCreateFormAsync()
    {
        ErrorMessage = string.Empty;
        DechargeNumber = "DCH-2024-0024";
        SelectedEmployee = null;
        ItemCondition = "Bon";
        Notes = string.Empty;
        NewDechargeItems.Clear();

        try
        {
            using var db = DatabaseInitializer.CreateDbContext();

            var employees = await db.Employees.OrderBy(e => e.FullName).ToListAsync();
            EmployeesList = new ObservableCollection<Employee>(employees);

            var availableEquipment = await db.Equipments
                .Where(e => e.Status == EquipmentStatus.Available)
                .OrderBy(e => e.Brand).ThenBy(e => e.Model)
                .ToListAsync();
            AvailableEquipmentsList = new ObservableCollection<Equipment>(availableEquipment);

            SelectedEquipmentToAdd = AvailableEquipmentsList.FirstOrDefault();
            IsCreateFormOpen = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public void AddItemToDecharge()
    {
        if (SelectedEquipmentToAdd == null) return;
        if (string.IsNullOrWhiteSpace(ItemCondition))
        {
            ItemCondition = "Neuf";
        }

        var itemModel = new NewDechargeItemModel
        {
            Equipment = SelectedEquipmentToAdd,
            ConditionAtAssignment = ItemCondition.Trim()
        };

        NewDechargeItems.Add(itemModel);
        AvailableEquipmentsList.Remove(SelectedEquipmentToAdd);
        SelectedEquipmentToAdd = AvailableEquipmentsList.FirstOrDefault();
    }

    [RelayCommand]
    public void RemoveItemFromDecharge(NewDechargeItemModel item)
    {
        if (item == null) return;
        NewDechargeItems.Remove(item);
        AvailableEquipmentsList.Add(item.Equipment);
        if (SelectedEquipmentToAdd == null)
        {
            SelectedEquipmentToAdd = item.Equipment;
        }
    }

    [RelayCommand]
    public void CancelCreateForm()
    {
        IsCreateFormOpen = false;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    public async Task SaveDechargeAsync()
    {
        if (string.IsNullOrWhiteSpace(DechargeNumber))
        {
            ErrorMessage = Loc["Common_Required"];
            return;
        }

        if (SelectedEmployee == null)
        {
            ErrorMessage = Loc["Dec_ErrorNoEmployee"];
            return;
        }

        if (NewDechargeItems.Count == 0)
        {
            ErrorMessage = Loc["Dec_ErrorNoItems"];
            return;
        }

        try
        {
            using var db = DatabaseInitializer.CreateDbContext();
            using var transaction = await db.Database.BeginTransactionAsync();

            // Validate decharge number uniqueness
            bool numberExists = await db.Decharges.AnyAsync(d => d.DechargeNumber.ToLower() == DechargeNumber.Trim().ToLower());
            if (numberExists)
            {
                ErrorMessage = Loc["Dec_ErrorNumberExists"];
                return;
            }

            var decharge = new Decharge
            {
                DechargeNumber = DechargeNumber.Trim(),
                EmployeeId = SelectedEmployee.Id,
                IssueDate = DateTime.UtcNow,
                Status = "Active",
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
            };

            db.Decharges.Add(decharge);
            await db.SaveChangesAsync();

            foreach (var itemModel in NewDechargeItems)
            {
                var dechargeItem = new DechargeItem
                {
                    DechargeId = decharge.Id,
                    EquipmentId = itemModel.Equipment.Id,
                    ConditionAtAssignment = itemModel.ConditionAtAssignment
                };
                db.DechargeItems.Add(dechargeItem);

                // Update equipment status to Assigned
                var eq = await db.Equipments.FindAsync(itemModel.Equipment.Id);
                if (eq != null)
                {
                    eq.Status = EquipmentStatus.Assigned;
                }
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            IsCreateFormOpen = false;
            await LoadDechargesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public void ViewDetails(DechargeSummaryModel summary)
    {
        if (summary == null) return;
        NavigateToDetailsRequested?.Invoke(summary.Id);
    }
}
