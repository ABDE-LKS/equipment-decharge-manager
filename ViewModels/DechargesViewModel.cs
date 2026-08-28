using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentDechargeManager.Data.Entities;
using EquipmentDechargeManager.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EquipmentDechargeManager.ViewModels;

public class DechargeSummaryItem
{
    public int Id { get; set; }
    public string DechargeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Matricule { get; set; } = string.Empty;
    public string Structure { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public int ItemCount { get; set; }
    public string Status { get; set; } = "ACTIVE";

    public string IssueDateText => IssueDate.ToString("dd/MM/yyyy");
}

public partial class NewDechargeItemModel : ObservableObject
{
    public Equipment Equipment { get; set; } = null!;

    [ObservableProperty]
    private string _conditionAtAssignment = "Neuf ";

    public string Type => Equipment?.Type ?? string.Empty;
    public string Brand => Equipment?.Brand ?? string.Empty;
    public string Model => Equipment?.Model ?? string.Empty;
    public string SerialNumber => Equipment?.DisplaySerialNumber ?? "—";
    public string InventoryNumber => Equipment?.DisplayInventoryNumber ?? "—";
    public string ShCode => Equipment?.DisplayShCode ?? "—";
    public string DisplayTitle => $"{Type} {Brand} {Model}".Trim();
}

public partial class DechargesViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<DechargeSummaryItem> _activeDecharges = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private Employee? _selectedEmployeeFilter;

    [ObservableProperty]
    private ObservableCollection<Employee> _employeesFilterList = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasNoDecharges;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _successMessage = string.Empty;

    // --- Create Form Properties ---
    [ObservableProperty]
    private bool _isCreateFormOpen;

    [ObservableProperty]
    private string _dechargeNumber = string.Empty;

    [ObservableProperty]
    private Employee? _selectedEmployee;

    [ObservableProperty]
    private ObservableCollection<Employee> _employeesList = new();

    [ObservableProperty]
    private ObservableCollection<Employee> _filteredEmployees = new();

    [ObservableProperty]
    private string _employeeSearchText = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _issueDate = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private string _searchEquipmentText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Equipment> _equipmentSearchResults = new();

    [ObservableProperty]
    private ObservableCollection<NewDechargeItemModel> _selectedEquipmentItems = new();

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isConfirmationOpen;

    private CancellationTokenSource? _equipmentSearchCts;

    public Action<int, bool>? NavigateToDetailsRequested { get; set; }

    public bool HasEmployeeFilterSelection => SelectedEmployeeFilter != null;
    public bool HasSelectedEmployee => SelectedEmployee != null;
    public string SelectedEmployeeMatricule => SelectedEmployee?.Matricule ?? "—";
    public string SelectedEmployeeStructure => SelectedEmployee?.Structure ?? "—";
    public string SelectedEmployeeFunction => SelectedEmployee?.Function ?? "—";

    public bool HasSelectedEquipmentItems => SelectedEquipmentItems.Count > 0;
    public bool HasNoSelectedEquipmentItems => SelectedEquipmentItems.Count == 0;
    public int SelectedEquipmentCount => SelectedEquipmentItems.Count;
    public bool CanSaveDecharge => SelectedEquipmentItems.Count > 0 && SelectedEmployee != null && IssueDate.HasValue && !IsSaving;

    public DechargesViewModel()
    {
        SelectedEquipmentItems.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(HasSelectedEquipmentItems));
            OnPropertyChanged(nameof(HasNoSelectedEquipmentItems));
            OnPropertyChanged(nameof(SelectedEquipmentCount));
            OnPropertyChanged(nameof(CanSaveDecharge));
        };
        _ = LoadDechargesAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadDechargesAsync();

    partial void OnSelectedEmployeeFilterChanged(Employee? value) => _ = LoadDechargesAsync();

    partial void OnEmployeeSearchTextChanged(string value) => RefreshFilteredEmployees();

    partial void OnSearchEquipmentTextChanged(string value) => _ = DebouncedSearchEquipmentAsync();

    partial void OnSelectedEmployeeChanged(Employee? value)
    {
        OnPropertyChanged(nameof(HasSelectedEmployee));
        OnPropertyChanged(nameof(SelectedEmployeeMatricule));
        OnPropertyChanged(nameof(SelectedEmployeeStructure));
        OnPropertyChanged(nameof(SelectedEmployeeFunction));
        OnPropertyChanged(nameof(CanSaveDecharge));
        if (value != null)
        {
            EmployeeSearchText = value.DisplayName;
        }
    }

    partial void OnIssueDateChanged(DateTimeOffset? value)
    {
        OnPropertyChanged(nameof(CanSaveDecharge));
    }

    partial void OnIsSavingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSaveDecharge));
    }

    [RelayCommand]
    public async Task LoadDechargesAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            using var db = DatabaseInitializer.CreateDbContext();

            // Load employees for filter dropdown
            var employees = await db.Employees.OrderBy(e => e.FullName).ToListAsync();
            EmployeesFilterList = new ObservableCollection<Employee>(employees);

            // Query PostgreSQL for ACTIVE décharges ONLY
            var query = db.Decharges
                .Include(d => d.Employee)
                .Include(d => d.Items)
                .Where(d => d.Status == "ACTIVE")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string term = SearchText.Trim().ToLower();
                query = query.Where(d =>
                    d.DechargeNumber.ToLower().Contains(term) ||
                    (d.Employee != null && d.Employee.FullName.ToLower().Contains(term)) ||
                    (d.Employee != null && d.Employee.Matricule.ToLower().Contains(term)) ||
                    (d.Employee != null && d.Employee.Structure.ToLower().Contains(term)));
            }

            if (SelectedEmployeeFilter != null)
            {
                int empId = SelectedEmployeeFilter.Id;
                query = query.Where(d => d.EmployeeId == empId);
            }

            var list = await query.OrderByDescending(d => d.IssueDate).ThenByDescending(d => d.Id).ToListAsync();

            var summaryList = list.Select(d => new DechargeSummaryItem
            {
                Id = d.Id,
                DechargeNumber = d.DechargeNumber,
                EmployeeName = d.Employee?.FullName ?? "—",
                Matricule = d.Employee?.Matricule ?? "—",
                Structure = d.Employee?.Structure ?? "—",
                IssueDate = d.IssueDate,
                ItemCount = d.Items.Count,
                Status = d.Status
            }).ToList();

            ActiveDecharges = new ObservableCollection<DechargeSummaryItem>(summaryList);
            HasNoDecharges = ActiveDecharges.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = "Erreur lors du chargement des décharges : " + ex.Message;
            ActiveDecharges.Clear();
            HasNoDecharges = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void ClearEmployeeFilter()
    {
        SelectedEmployeeFilter = null;
    }

    [RelayCommand]
    public void ViewDetails(DechargeSummaryItem summary)
    {
        if (summary == null) return;
        NavigateToDetailsRequested?.Invoke(summary.Id, false);
    }

    [RelayCommand]
    public void RestituerDecharge(DechargeSummaryItem summary)
    {
        if (summary == null) return;
        NavigateToDetailsRequested?.Invoke(summary.Id, true);
    }

    [RelayCommand]
    public async Task OpenCreateFormAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        ValidationMessage = string.Empty;
        IsConfirmationOpen = false;
        IsSaving = false;

        DechargeNumber = await GenerateDechargeNumberAsync();
        IssueDate = DateTimeOffset.Now.Date;
        SelectedEmployee = null;
        EmployeeSearchText = string.Empty;
        Notes = string.Empty;
        SearchEquipmentText = string.Empty;
        SelectedEquipmentItems.Clear();
        EquipmentSearchResults.Clear();

        try
        {
            using var db = DatabaseInitializer.CreateDbContext();
            var employees = await db.Employees.OrderBy(e => e.FullName).ToListAsync();
            EmployeesList = new ObservableCollection<Employee>(employees);
            RefreshFilteredEmployees();

            IsCreateFormOpen = true;
            await DebouncedSearchEquipmentAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Impossible d'ouvrir le formulaire : " + ex.Message;
        }
    }

    [RelayCommand]
    public void CancelCreateForm()
    {
        _equipmentSearchCts?.Cancel();
        IsCreateFormOpen = false;
        IsConfirmationOpen = false;
        IsSaving = false;
        ErrorMessage = string.Empty;
        ValidationMessage = string.Empty;
        EquipmentSearchResults.Clear();
        SelectedEquipmentItems.Clear();
    }

    private void RefreshFilteredEmployees()
    {
        string term = EmployeeSearchText.Trim();

        IEnumerable<Employee> employees = string.IsNullOrWhiteSpace(term)
            ? EmployeesList
            : EmployeesList.Where(e =>
                e.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Matricule.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Function.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase));

        FilteredEmployees = new ObservableCollection<Employee>(employees.OrderBy(e => e.FullName));
    }

    private async Task DebouncedSearchEquipmentAsync()
    {
        _equipmentSearchCts?.Cancel();
        _equipmentSearchCts = new CancellationTokenSource();
        var token = _equipmentSearchCts.Token;

        string term = SearchEquipmentText.Trim();
        try
        {
            await Task.Delay(200, token);

            using var db = DatabaseInitializer.CreateDbContext();
            var selectedIds = SelectedEquipmentItems.Select(i => i.Equipment.Id).ToList();

            // Query ONLY AVAILABLE equipment
            IQueryable<Equipment> query = db.Equipments
                .Where(e => e.Status == EquipmentStatus.Available)
                .Where(e => !selectedIds.Contains(e.Id));

            if (!string.IsNullOrWhiteSpace(term))
            {
                string lowered = term.ToLower();
                query = query.Where(e =>
                    e.Type.ToLower().Contains(lowered) ||
                    e.Brand.ToLower().Contains(lowered) ||
                    e.Model.ToLower().Contains(lowered) ||
                    (e.SerialNumber != null && e.SerialNumber.ToLower().Contains(lowered)) ||
                    (e.InventoryNumber != null && e.InventoryNumber.ToLower().Contains(lowered)) ||
                    (e.ShCode != null && e.ShCode.ToLower().Contains(lowered)));
            }

            var results = await query
                .OrderBy(e => e.Type)
                .ThenBy(e => e.Brand)
                .ThenBy(e => e.Model)
                .Take(25)
                .ToListAsync(token);

            if (token.IsCancellationRequested) return;

            EquipmentSearchResults = new ObservableCollection<Equipment>(results);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public void AddEquipment(Equipment equipment)
    {
        if (equipment == null) return;

        if (SelectedEquipmentItems.Any(i => i.Equipment.Id == equipment.Id))
        {
            ValidationMessage = "Cet équipement est déjà dans la liste des équipements sélectionnés.";
            return;
        }

        if (equipment.Status != EquipmentStatus.Available)
        {
            ValidationMessage = "Cet équipement n'est pas disponible.";
            return;
        }

        SelectedEquipmentItems.Add(new NewDechargeItemModel
        {
            Equipment = equipment,
            ConditionAtAssignment = "Neuf "
        });

        EquipmentSearchResults.Remove(equipment);
        ValidationMessage = string.Empty;
    }

    [RelayCommand]
    public void RemoveEquipment(NewDechargeItemModel item)
    {
        if (item == null) return;
        SelectedEquipmentItems.Remove(item);
        ValidationMessage = string.Empty;
        _ = DebouncedSearchEquipmentAsync();
    }

    [RelayCommand]
    public void ConfirmCreate()
    {
        ErrorMessage = string.Empty;
        ValidationMessage = string.Empty;

        if (SelectedEmployee == null)
        {
            ValidationMessage = "Veuillez sélectionner un employé.";
            return;
        }

        if (!IssueDate.HasValue)
        {
            ValidationMessage = "La date de la décharge est requise.";
            return;
        }

        if (SelectedEquipmentItems.Count == 0)
        {
            ValidationMessage = "Veuillez sélectionner au moins un équipement.";
            return;
        }

        IsConfirmationOpen = true;
    }

    [RelayCommand]
    public void CancelConfirmation()
    {
        IsConfirmationOpen = false;
    }

    private async Task<string> GenerateDechargeNumberAsync()
    {
        var now = DateTime.Today;
        var prefix = $"DCH-{now:yyyy}-{now:MMdd}-";

        using var db = DatabaseInitializer.CreateDbContext();
        var existingNumbers = await db.Decharges
            .Where(d => d.DechargeNumber.StartsWith(prefix))
            .Select(d => d.DechargeNumber)
            .ToListAsync();

        int maxSeq = 0;
        foreach (var num in existingNumbers)
        {
            if (num.Length > prefix.Length && int.TryParse(num.Substring(prefix.Length), out int seq) && seq > maxSeq)
            {
                maxSeq = seq;
            }
        }

        int nextSeq = maxSeq + 1;
        string candidate;
        do
        {
            candidate = $"{prefix}{nextSeq:000}";
            nextSeq++;
        }
        while (await db.Decharges.AnyAsync(d => d.DechargeNumber.ToLower() == candidate.ToLower()));

        return candidate;
    }

    [RelayCommand]
    public async Task SaveDechargeAsync()
    {
        if (IsSaving) return;

        ErrorMessage = string.Empty;
        ValidationMessage = string.Empty;

        if (SelectedEmployee == null || !IssueDate.HasValue || SelectedEquipmentItems.Count == 0)
        {
            IsConfirmationOpen = false;
            ValidationMessage = "Vérifiez que tous les champs obligatoires (Employé, Date, Équipements) sont remplis.";
            return;
        }

        IsSaving = true;
        using var db = DatabaseInitializer.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var employee = await db.Employees.FindAsync(SelectedEmployee!.Id);
            if (employee == null)
            {
                ErrorMessage = "L'employé sélectionné n'existe plus.";
                IsConfirmationOpen = false;
                return;
            }

            var normalizedNumber = DechargeNumber.Trim();
            while (await db.Decharges.AnyAsync(d => d.DechargeNumber.ToLower() == normalizedNumber.ToLower()))
            {
                DechargeNumber = await GenerateDechargeNumberAsync();
                normalizedNumber = DechargeNumber;
            }

            var nowUtc = DateTime.UtcNow;
            var issueDateOnly = DateOnly.FromDateTime(IssueDate!.Value.DateTime);

            var equipmentIds = SelectedEquipmentItems.Select(i => i.Equipment.Id).Distinct().ToList();
            var dbEquipments = await db.Equipments
                .Where(e => equipmentIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id);

            foreach (var itemModel in SelectedEquipmentItems)
            {
                if (!dbEquipments.TryGetValue(itemModel.Equipment.Id, out var eq) || eq.Status != EquipmentStatus.Available)
                {
                    ErrorMessage = $"L'équipement '{itemModel.Equipment.Type} ({itemModel.Equipment.DisplaySerialNumber})' n'est plus disponible.";
                    await transaction.RollbackAsync();
                    IsConfirmationOpen = false;
                    return;
                }
            }

            var decharge = new Decharge
            {
                DechargeNumber = normalizedNumber,
                EmployeeId = employee.Id,
                IssueDate = issueDateOnly,
                Status = "ACTIVE",
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
            };

            db.Decharges.Add(decharge);
            await db.SaveChangesAsync();

            foreach (var itemModel in SelectedEquipmentItems)
            {
                var dechargeItem = new DechargeItem
                {
                    DechargeId = decharge.Id,
                    EquipmentId = itemModel.Equipment.Id,
                    ConditionAtAssignment = string.IsNullOrWhiteSpace(itemModel.ConditionAtAssignment)
                        ? "Neuf"
                        : itemModel.ConditionAtAssignment.Trim(),
                    AssignmentDate = issueDateOnly,
                    ReturnDate = null,
                    ConditionReturned = null
                };
                db.DechargeItems.Add(dechargeItem);

                var eq = dbEquipments[itemModel.Equipment.Id];
                eq.Status = EquipmentStatus.Assigned;
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            IsConfirmationOpen = false;
            IsCreateFormOpen = false;
            SuccessMessage = $"✓ Décharge {normalizedNumber} créée avec succès";

            await LoadDechargesAsync();
        }
        catch (Exception ex)
        {
            // Walk the full inner exception chain to surface the real database error
            var inner = ex;
            while (inner.InnerException != null) inner = inner.InnerException;
            string detail = inner == ex ? ex.Message : $"{ex.Message} — Détail : {inner.Message}";
            ErrorMessage = "Erreur d'enregistrement : " + detail;
            try { await transaction.RollbackAsync(); } catch { }
        }
        finally
        {
            IsSaving = false;
        }
    }
}
