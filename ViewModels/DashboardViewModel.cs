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

public partial class DashboardViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _totalEquipmentCount;

    [ObservableProperty]
    private int _availableCount;

    [ObservableProperty]
    private int _assignedCount;

    [ObservableProperty]
    private int _returnedCount;

    [ObservableProperty]
    private int _damagedCount;

    [ObservableProperty]
    private int _lostCount;

    [ObservableProperty]
    private int _totalEmployeesCount;

    [ObservableProperty]
    private int _totalDechargesCount;

    [ObservableProperty]
    private int _activeItemsCount;

    [ObservableProperty]
    private ObservableCollection<DechargeSummaryModel> _recentDecharges = new();

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public Action<int>? NavigateToDetailsRequested { get; set; }

    public LocalizationManager Loc => LocalizationManager.Instance;

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
            ReturnedCount = await db.Equipments.CountAsync(e => e.Status == EquipmentStatus.Returned);
            DamagedCount = await db.Equipments.CountAsync(e => e.Status == EquipmentStatus.Damaged);
            LostCount = await db.Equipments.CountAsync(e => e.Status == EquipmentStatus.Lost);

            TotalEmployeesCount = await db.Employees.CountAsync();
            TotalDechargesCount = await db.Decharges.CountAsync();
            ActiveItemsCount = await db.DechargeItems.CountAsync(i => i.ReturnRecord == null);

            var recent = await db.Decharges
                .Include(d => d.Employee)
                .Include(d => d.Items)
                    .ThenInclude(i => i.ReturnRecord)
                .OrderByDescending(d => d.IssueDate)
                .Take(5)
                .ToListAsync();

            var recentSummaries = recent.Select(d => new DechargeSummaryModel
            {
                Id = d.Id,
                DechargeNumber = d.DechargeNumber,
                EmployeeName = d.Employee.FullName,
                IssueDate = d.IssueDate,
                ItemCount = d.Items.Count,
                ReturnedItemCount = d.Items.Count(i => i.ReturnRecord != null)
            }).ToList();

            RecentDecharges = new ObservableCollection<DechargeSummaryModel>(recentSummaries);
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
