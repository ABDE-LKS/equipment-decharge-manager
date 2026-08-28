using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentDechargeManager.Data.Entities;
using EquipmentDechargeManager.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace EquipmentDechargeManager.ViewModels;

public class DashboardDechargeSummary
{
    public int Id { get; set; }
    public string DechargeNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public int ItemCount { get; set; }
    public string Status { get; set; } = "ACTIVE";

    public string IssueDateText => IssueDate.ToString("dd/MM/yyyy");

    public string StatusText => Status.ToUpper() switch
    {
        "ACTIVE" => "Active",
        "RETOURNÉE" => "Retournée",
        _ => Status
    };

    public string StatusBgColor => Status.ToUpper() switch
    {
        "ACTIVE" => "#FEF3C7",
        "RETOURNÉE" => "#ECFDF5",
        _ => "#F1F5F9"
    };

    public string StatusFgColor => Status.ToUpper() switch
    {
        "ACTIVE" => "#D97706",
        "RETOURNÉE" => "#059669",
        _ => "#64748B"
    };
}

public partial class DashboardViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _totalEquipmentCount;

    [ObservableProperty]
    private int _availableCount;

    [ObservableProperty]
    private int _assignedCount;

    [ObservableProperty]
    private int _totalEmployeesCount;

    [ObservableProperty]
    private int _totalDechargesCount;

    [ObservableProperty]
    private int _activeItemsCount;

    [ObservableProperty]
    private ObservableCollection<DashboardDechargeSummary> _recentDecharges = new();

    [ObservableProperty]
    private bool _isTableExpanded = false;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public Action<int>? NavigateToDetailsRequested { get; set; }

    public DashboardViewModel()
    {
        _ = RefreshDashboardAsync();
    }

    [RelayCommand]
    public async Task RefreshDashboardAsync()
    {
        try
        {
            using var db = DatabaseInitializer.CreateDbContext();

            TotalEquipmentCount = await db.Equipments.CountAsync();
            AvailableCount = await db.Equipments.CountAsync(e => e.Status == EquipmentStatus.Available);
            AssignedCount = await db.Equipments.CountAsync(e => e.Status == EquipmentStatus.Assigned);

            TotalEmployeesCount = await db.Employees.CountAsync();
            TotalDechargesCount = await db.Decharges.CountAsync();
            ActiveItemsCount = await db.DechargeItems.CountAsync(i => i.ReturnDate == null);

            var recent = await db.Decharges
                .Include(d => d.Employee)
                .Include(d => d.Items)
                .OrderByDescending(d => d.IssueDate)
                .ThenByDescending(d => d.Id)
                .Take(IsTableExpanded ? int.MaxValue : 5)
                .ToListAsync();

            var recentSummaries = recent.Select(d => new DashboardDechargeSummary
            {
                Id = d.Id,
                DechargeNumber = d.DechargeNumber,
                EmployeeName = d.Employee?.FullName ?? "—",
                IssueDate = d.IssueDate,
                ItemCount = d.Items.Count,
                Status = d.Status
            }).ToList();

            RecentDecharges = new ObservableCollection<DashboardDechargeSummary>(recentSummaries);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public void ViewDetails(DashboardDechargeSummary summary)
    {
        if (summary == null) return;
        NavigateToDetailsRequested?.Invoke(summary.Id);
    }

    [RelayCommand]
    public async Task ToggleTableExpansion()
    {
        IsTableExpanded = !IsTableExpanded;
        await RefreshDashboardAsync();
    }
}
