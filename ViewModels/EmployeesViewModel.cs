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

    // Form Properties
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

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public LocalizationManager Loc => LocalizationManager.Instance;

    public EmployeesViewModel()
    {
        _ = LoadEmployeesAsync();
    }

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
                query = query.Where(e => e.FullName.ToLower().Contains(term) || e.Matricule.ToLower().Contains(term));
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
        FormTitle = Loc["Emp_AddTitle"];
        FullName = string.Empty;
        Matricule = string.Empty;
        Function = string.Empty;
        Structure = string.Empty;
        Region = string.Empty;
        ErrorMessage = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand]
    public void OpenEditForm(Employee employee)
    {
        if (employee == null) return;
        EditingEmployeeId = employee.Id;
        FormTitle = Loc["Emp_EditTitle"];
        FullName = employee.FullName;
        Matricule = employee.Matricule;
        Function = employee.Function;
        Structure = employee.Structure;
        Region = employee.Region;
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
        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Matricule))
        {
            ErrorMessage = Loc["Common_Required"];
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
                ErrorMessage = Loc["Emp_ErrorMatriculeExists"];
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
            await LoadEmployeesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public async Task DeleteEmployeeAsync(Employee employee)
    {
        if (employee == null) return;
        try
        {
            using var db = DatabaseInitializer.CreateDbContext();
            var emp = await db.Employees.FindAsync(employee.Id);
            if (emp != null)
            {
                db.Employees.Remove(emp);
                await db.SaveChangesAsync();
                await LoadEmployeesAsync();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
