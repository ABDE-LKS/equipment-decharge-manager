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

public class HistoryEquipmentDetailModel
{
    public string Type { get; set; } = "—";
    public string Brand { get; set; } = "—";
    public string Model { get; set; } = "—";
    public string SerialNumber { get; set; } = "—";
    public string InventoryNumber { get; set; } = "—";
    public string ShCode { get; set; } = "—";
    public string ConditionReturned { get; set; } = "—";

    public string DisplayLabel => $"{Type} {Brand} {Model}".Trim();
}

public class HistoryRecordItem
{
    public int DechargeId { get; set; }
    public string DechargeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Matricule { get; set; } = string.Empty;
    public string Structure { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public int ItemCount { get; set; }
    public string EquipmentSummary { get; set; } = string.Empty;
    public List<HistoryEquipmentDetailModel> EquipmentDetails { get; set; } = new();
    public string Status { get; set; } = "RETOURNÉE";

    public string IssueDateText => IssueDate.ToString("dd/MM/yyyy");
    public string ReturnDateText => ReturnDate.HasValue ? ReturnDate.Value.ToString("dd/MM/yyyy") : "—";
}

public partial class HistoryViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<HistoryRecordItem> _historyItems = new();

    [ObservableProperty]
    private ObservableCollection<Employee> _employees = new();

    [ObservableProperty]
    private Employee? _selectedEmployee;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasNoHistory;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isDetailsModalOpen;

    [ObservableProperty]
    private HistoryRecordItem? _selectedHistoryItem;

    public Action<int>? NavigateToDetailsRequested { get; set; }

    public bool HasEmployeeSelection => SelectedEmployee != null;

    public HistoryViewModel()
    {
        _ = LoadHistoryAsync();
    }

    partial void OnSelectedEmployeeChanged(Employee? value) => _ = LoadHistoryAsync();

    partial void OnSearchTextChanged(string value) => _ = LoadHistoryAsync();

    [RelayCommand]
    public void ClearEmployeeFilter()
    {
        SelectedEmployee = null;
    }

    [RelayCommand]
    public void OpenDetailsModal(HistoryRecordItem item)
    {
        if (item == null) return;
        SelectedHistoryItem = item;
        IsDetailsModalOpen = true;
    }

    [RelayCommand]
    public void CloseDetailsModal()
    {
        IsDetailsModalOpen = false;
        SelectedHistoryItem = null;
    }

    [RelayCommand]
    public void ViewDetails(HistoryRecordItem item)
    {
        if (item != null && item.DechargeId > 0)
        {
            NavigateToDetailsRequested?.Invoke(item.DechargeId);
        }
    }

    [RelayCommand]
    public async Task LoadHistoryAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            using var db = DatabaseInitializer.CreateDbContext();

            // Load employees list for dropdown filter
            var employeesList = await db.Employees.OrderBy(e => e.FullName).ToListAsync();
            Employees = new ObservableCollection<Employee>(employeesList);

            // Query PostgreSQL for ONLY RETOURNÉE / COMPLETED décharges
            var query = db.Decharges
                .Include(d => d.Employee)
                .Include(d => d.Items)
                    .ThenInclude(i => i.Equipment)
                .Where(d => d.Status == "RETOURNÉE" || d.Status == "COMPLETED")
                .AsQueryable();

            if (SelectedEmployee != null)
            {
                int empId = SelectedEmployee.Id;
                query = query.Where(d => d.EmployeeId == empId);
            }

            var dechargesList = await query
                .OrderByDescending(d => d.IssueDate)
                .ThenByDescending(d => d.Id)
                .ToListAsync();

            var records = new List<HistoryRecordItem>();

            foreach (var d in dechargesList)
            {
                var equipmentDetailsList = d.Items.Select(i => new HistoryEquipmentDetailModel
                {
                    Type = i.Equipment?.Type ?? "—",
                    Brand = i.Equipment?.Brand ?? "—",
                    Model = i.Equipment?.Model ?? "—",
                    SerialNumber = i.Equipment?.DisplaySerialNumber ?? "—",
                    InventoryNumber = i.Equipment?.DisplayInventoryNumber ?? "—",
                    ShCode = i.Equipment?.DisplayShCode ?? "—",
                    ConditionReturned = string.IsNullOrWhiteSpace(i.ConditionReturned) ? "Bon état" : i.ConditionReturned
                }).ToList();

                var maxReturnDate = d.Items
                    .Where(i => i.ReturnDate.HasValue)
                    .Select(i => i.ReturnDate!.Value)
                    .OrderByDescending(dt => dt)
                    .FirstOrDefault();

                var equipmentSummaryNames = equipmentDetailsList
                    .Select(e => e.DisplayLabel)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .ToList();

                string summaryText = equipmentSummaryNames.Count > 0
                    ? string.Join(", ", equipmentSummaryNames)
                    : "(Aucun équipement)";

                records.Add(new HistoryRecordItem
                {
                    DechargeId = d.Id,
                    DechargeNumber = d.DechargeNumber,
                    EmployeeName = d.Employee?.FullName ?? "—",
                    Matricule = d.Employee?.Matricule ?? "—",
                    Structure = d.Employee?.Structure ?? "—",
                    IssueDate = d.IssueDate,
                    ReturnDate = maxReturnDate != default ? maxReturnDate : d.IssueDate,
                    ItemCount = d.Items.Count,
                    EquipmentSummary = summaryText,
                    EquipmentDetails = equipmentDetailsList,
                    Status = "RETOURNÉE"
                });
            }

            // Search filter in memory if term specified
            IEnumerable<HistoryRecordItem> filtered = records;
            string term = SearchText?.Trim().ToLower() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(term))
            {
                filtered = filtered.Where(r =>
                    r.DechargeNumber.ToLower().Contains(term) ||
                    r.EmployeeName.ToLower().Contains(term) ||
                    r.Matricule.ToLower().Contains(term) ||
                    r.Structure.ToLower().Contains(term) ||
                    r.EquipmentSummary.ToLower().Contains(term) ||
                    r.EquipmentDetails.Any(e =>
                        e.Type.ToLower().Contains(term) ||
                        e.Brand.ToLower().Contains(term) ||
                        e.Model.ToLower().Contains(term) ||
                        e.SerialNumber.ToLower().Contains(term) ||
                        e.InventoryNumber.ToLower().Contains(term) ||
                        e.ShCode.ToLower().Contains(term)));
            }

            HistoryItems = new ObservableCollection<HistoryRecordItem>(filtered
                .OrderByDescending(r => r.ReturnDate)
                .ThenByDescending(r => r.DechargeId));

            HasNoHistory = HistoryItems.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = "Erreur lors du chargement de l'historique : " + ex.Message;
            HistoryItems.Clear();
            HasNoHistory = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
