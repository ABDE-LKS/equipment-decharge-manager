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

public partial class EmployeesViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Employee> _employees = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    // Form Properties (Add / Edit)
    [ObservableProperty]
    private bool _isFormOpen;

    [ObservableProperty]
    private string _formTitle = string.Empty;

    [ObservableProperty]
    private int? _editingEmployeeId;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _matricule = string.Empty;

    [ObservableProperty]
    private string _function = string.Empty;

    [ObservableProperty]
    private string _structure = string.Empty;

    [ObservableProperty]
    private string _region = string.Empty;

    // Delete Confirmation Modal Properties
    [ObservableProperty]
    private bool _isDeleteConfirmOpen;

    [ObservableProperty]
    private Employee? _employeeToDelete;

    [ObservableProperty]
    private int _relatedDechargeCount;

    [ObservableProperty]
    private bool _canDeleteEmployee;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _successMessage = string.Empty;

    public EmployeesViewModel(string? initialSearch = null)
    {
        if (!string.IsNullOrWhiteSpace(initialSearch))
            SearchText = initialSearch;

        _ = LoadEmployeesAsync();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadEmployeesAsync();

    [RelayCommand]
    public async Task LoadEmployeesAsync()
    {
        try
        {
            using var db = DatabaseInitializer.CreateDbContext();
            var query = db.Employees.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string term = SearchText.Trim().ToLower();
                query = query.Where(e => e.FullName.ToLower().Contains(term) || e.Matricule.ToLower().Contains(term) || e.Structure.ToLower().Contains(term));
            }

            var list = await query.OrderBy(e => e.FullName).ToListAsync();
            Employees = new ObservableCollection<Employee>(list);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public void OpenAddForm()
    {
        EditingEmployeeId = null;
        FormTitle = "Ajouter un employé";
        FullName = string.Empty;
        Matricule = string.Empty;
        Function = string.Empty;
        Structure = string.Empty;
        Region = string.Empty;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand]
    public void OpenEditForm(Employee employee)
    {
        if (employee == null) return;
        EditingEmployeeId = employee.Id;
        FormTitle = "Modifier l'employé";
        FullName = employee.FullName;
        Matricule = employee.Matricule;
        Function = employee.Function;
        Structure = employee.Structure;
        Region = employee.Region;
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
        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Matricule))
        {
            ErrorMessage = "Champ obligatoire";
            return;
        }

        try
        {
            using var db = DatabaseInitializer.CreateDbContext();

            // Unique matricule validation
            bool exists = await db.Employees.AnyAsync(e =>
                e.Matricule.ToLower() == Matricule.Trim().ToLower() &&
                (!EditingEmployeeId.HasValue || e.Id != EditingEmployeeId.Value));

            if (exists)
            {
                ErrorMessage = "Ce matricule existe déjà.";
                return;
            }

            if (EditingEmployeeId.HasValue)
            {
                var emp = await db.Employees.FindAsync(EditingEmployeeId.Value);
                if (emp != null)
                {
                    emp.FullName = FullName.Trim();
                    emp.Matricule = Matricule.Trim();
                    emp.Function = Function.Trim();
                    emp.Structure = Structure.Trim();
                    emp.Region = Region.Trim();
                }
            }
            else
            {
                var emp = new Employee
                {
                    FullName = FullName.Trim(),
                    Matricule = Matricule.Trim(),
                    Function = Function.Trim(),
                    Structure = Structure.Trim(),
                    Region = Region.Trim()
                };
                db.Employees.Add(emp);
            }

            await db.SaveChangesAsync();
            IsFormOpen = false;
            SuccessMessage = $"✓ Employé {FullName.Trim()} enregistré avec succès";
            await LoadEmployeesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public async Task OpenDeleteConfirmationAsync(Employee employee)
    {
        if (employee == null) return;

        EmployeeToDelete = employee;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            using var db = DatabaseInitializer.CreateDbContext();
            // Check if employee has any ACTIVE décharge
            int activeDechargeCount = await db.Decharges.CountAsync(d => d.EmployeeId == employee.Id && d.Status == "ACTIVE");
            CanDeleteEmployee = activeDechargeCount == 0;
            IsDeleteConfirmOpen = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = "Impossible de vérifier les décharges associées : " + ex.Message;
        }
    }

    [RelayCommand]
    public void CancelDeleteConfirmation()
    {
        IsDeleteConfirmOpen = false;
        EmployeeToDelete = null;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    public async Task ConfirmDeleteEmployeeAsync()
    {
        if (EmployeeToDelete == null || !CanDeleteEmployee) return;

        try
        {
            using var db = DatabaseInitializer.CreateDbContext();
            var emp = await db.Employees.FindAsync(EmployeeToDelete.Id);
            if (emp != null)
            {
                string empName = emp.FullName;
                db.Employees.Remove(emp);
                await db.SaveChangesAsync();

                IsDeleteConfirmOpen = false;
                EmployeeToDelete = null;
                SuccessMessage = $"✓ Employé '{empName}' supprimé avec succès";
                await LoadEmployeesAsync();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Erreur lors de la suppression : " + ex.Message;
        }
    }
}
