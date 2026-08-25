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

public partial class DechargeItemDisplayModel : ObservableObject
{
    public DechargeItem Item { get; set; } = null!;
    
    public string EquipmentName { get; set; } = string.Empty;
    public string InventoryNumber { get; set; } = string.Empty;
    public string ConditionAtAssignment { get; set; } = "Bon";
    public bool IsReturned { get; set; }
    public string ReturnDateText { get; set; } = string.Empty;
    public string ReturnConditionText { get; set; } = string.Empty;

    public string StatusText => IsReturned ? "Retourné" : "Non retourné";
    public string StatusBgColor => IsReturned ? "#F0FDFA" : "#F1F5F9";
    public string StatusFgColor => IsReturned ? "#06B6D4" : "#64748B";

    public string EquipmentSummary => $"{EquipmentName} ({InventoryNumber})";
}

public partial class DechargeDetailsViewModel : ViewModelBase
{
    private readonly int _dechargeId;

    [ObservableProperty]
    private Decharge? _decharge;

    [ObservableProperty]
    private string _dechargeNumber = string.Empty;

    [ObservableProperty]
    private string _employeeFullName = string.Empty;

    [ObservableProperty]
    private string _issueDateText = string.Empty;

    [ObservableProperty]
    private string _notesText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DechargeItemDisplayModel> _itemsList = new();

    // Return Form Modal
    [ObservableProperty]
    private bool _isReturnFormOpen;

    [ObservableProperty]
    private DechargeItemDisplayModel? _selectedItemToReturn;

    [ObservableProperty]
    private DateTimeOffset _returnDate = DateTimeOffset.Now;

    [ObservableProperty]
    private string _returnCondition = "Bon";

    [ObservableProperty]
    private EquipmentStatus _selectedNewEquipmentStatus = EquipmentStatus.Available;

    [ObservableProperty]
    private string _returnNotes = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public List<EquipmentStatus> ReturnStatusOptions { get; } = new()
    {
        EquipmentStatus.Available,
        EquipmentStatus.Returned,
        EquipmentStatus.Damaged,
        EquipmentStatus.Lost
    };

    public Action? BackRequested { get; set; }
    public Action<Decharge>? OpenPdfPreviewRequested { get; set; }

    public LocalizationManager Loc => LocalizationManager.Instance;

    [RelayCommand]
    public void OpenPdfPreview()
    {
        if (Decharge != null)
        {
            OpenPdfPreviewRequested?.Invoke(Decharge);
        }
    }

    public DechargeDetailsViewModel(int dechargeId)
    {
        _dechargeId = dechargeId;
        _ = LoadDechargeDetailsAsync();
    }

    [RelayCommand]
    public async Task LoadDechargeDetailsAsync()
    {
        try
        {
            using var db = DatabaseInitializer.CreateDbContext();
            var d = await db.Decharges
                .Include(x => x.Employee)
                .Include(x => x.Items)
                    .ThenInclude(i => i.Equipment)
                .Include(x => x.Items)
                    .ThenInclude(i => i.ReturnRecord)
                .FirstOrDefaultAsync(x => x.Id == _dechargeId);

            if (d != null)
            {
                Decharge = d;
                DechargeNumber = d.DechargeNumber;
                EmployeeFullName = $"{d.Employee.FullName} ({d.Employee.Matricule})";
                IssueDateText = d.IssueDate.ToString("dd/MM/yyyy");
                NotesText = d.Notes ?? string.Empty;

                var displayList = d.Items.Select(i => new DechargeItemDisplayModel
                {
                    Item = i,
                    EquipmentName = $"{i.Equipment.Type} - {i.Equipment.Brand} {i.Equipment.Model}",
                    InventoryNumber = i.Equipment.InventoryNumber,
                    ConditionAtAssignment = i.ConditionAtAssignment,
                    IsReturned = i.ReturnRecord != null,
                    ReturnDateText = i.ReturnRecord != null ? i.ReturnRecord.ReturnDate.ToString("dd/MM/yyyy") : string.Empty,
                    ReturnConditionText = i.ReturnRecord?.ConditionReturned ?? string.Empty
                }).ToList();

                ItemsList = new ObservableCollection<DechargeItemDisplayModel>(displayList);
            }
            else
            {
                ErrorMessage = "Décharge introuvable.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public void OpenReturnForm(DechargeItemDisplayModel displayModel)
    {
        if (displayModel == null || displayModel.IsReturned) return;

        SelectedItemToReturn = displayModel;
        ReturnDate = DateTimeOffset.Now;
        ReturnCondition = "Bon état";
        SelectedNewEquipmentStatus = EquipmentStatus.Available;
        ReturnNotes = string.Empty;
        ErrorMessage = string.Empty;
        IsReturnFormOpen = true;
    }

    [RelayCommand]
    public void CancelReturnForm()
    {
        IsReturnFormOpen = false;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    public async Task SaveReturnAsync()
    {
        if (SelectedItemToReturn == null) return;
        if (string.IsNullOrWhiteSpace(ReturnCondition))
        {
            ErrorMessage = Loc["Common_Required"];
            return;
        }

        try
        {
            using var db = DatabaseInitializer.CreateDbContext();
            using var transaction = await db.Database.BeginTransactionAsync();

            var item = await db.DechargeItems
                .Include(di => di.Equipment)
                .Include(di => di.ReturnRecord)
                .FirstOrDefaultAsync(di => di.Id == SelectedItemToReturn.Item.Id);

            if (item == null) return;

            if (item.ReturnRecord != null)
            {
                ErrorMessage = Loc["DecDet_AlreadyReturned"];
                return;
            }

            var returnRecord = new EquipmentReturn
            {
                DechargeItemId = item.Id,
                ReturnDate = ReturnDate.UtcDateTime,
                ConditionReturned = ReturnCondition.Trim()
            };
            db.EquipmentReturns.Add(returnRecord);

            // Update equipment status
            item.Equipment.Status = SelectedNewEquipmentStatus;

            // Check if all items in decharge are returned to update Decharge.Status
            var decharge = await db.Decharges
                .Include(d => d.Items)
                    .ThenInclude(i => i.ReturnRecord)
                .FirstOrDefaultAsync(d => d.Id == _dechargeId);

            if (decharge != null)
            {
                int returnedCount = decharge.Items.Count(i => i.ReturnRecord != null || i.Id == item.Id);
                if (returnedCount == decharge.Items.Count)
                {
                    decharge.Status = "Retournée";
                }
                else if (returnedCount > 0)
                {
                    decharge.Status = "Partiellement retournée";
                }
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            IsReturnFormOpen = false;
            await LoadDechargeDetailsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public void GoBack()
    {
        BackRequested?.Invoke();
    }
}
