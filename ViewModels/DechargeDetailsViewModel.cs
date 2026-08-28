using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentDechargeManager.Data.Entities;
using EquipmentDechargeManager.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EquipmentDechargeManager.ViewModels;

public class DechargeDetailsItemModel
{
    public int ItemId { get; set; }
    public int EquipmentId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = "—";
    public string InventoryNumber { get; set; } = "—";
    public string ShCode { get; set; } = "—";
    public DateOnly AssignmentDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public string ConditionAtAssignment { get; set; } = "—";
    public string ConditionReturned { get; set; } = "—";
    public EquipmentStatus EquipmentStatus { get; set; }

    public string AssignmentDateText => AssignmentDate.ToString("dd/MM/yyyy");
    public string ReturnDateText => ReturnDate.HasValue ? ReturnDate.Value.ToString("dd/MM/yyyy") : "—";
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
    private string _employeeMatricule = string.Empty;

    [ObservableProperty]
    private string _employeeFunction = string.Empty;

    [ObservableProperty]
    private string _employeeStructure = string.Empty;

    [ObservableProperty]
    private string _employeeRegion = string.Empty;

    [ObservableProperty]
    private int _employeeId;

    [ObservableProperty]
    private string _issueDateText = string.Empty;

    [ObservableProperty]
    private string _notesText = string.Empty;

    [ObservableProperty]
    private string _status = "ACTIVE";

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private bool _isReturned;

    [ObservableProperty]
    private ObservableCollection<DechargeDetailsItemModel> _items = new();

    [ObservableProperty]
    private bool _isReturnModalOpen;

    [ObservableProperty]
    private DateTimeOffset _returnDate = DateTimeOffset.Now;

    [ObservableProperty]
    private string _returnCondition = "Bon état ";

    [ObservableProperty]
    private string _returnNotes = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _successMessage = string.Empty;

    public Action? BackRequested { get; set; }
    public Action<int, string>? ViewEmployeeRequested { get; set; }
    public Action<int, string>? ViewEquipmentRequested { get; set; }

    public DechargeDetailsViewModel(int dechargeId, bool autoOpenReturnModal = false)
    {
        _dechargeId = dechargeId;
        _ = InitializeAsync(autoOpenReturnModal);
    }

    private async Task InitializeAsync(bool autoOpenReturnModal)
    {
        await LoadDechargeDetailsAsync();
        if (autoOpenReturnModal && IsActive)
        {
            OpenReturnModal();
        }
    }

    [RelayCommand]
    public async Task LoadDechargeDetailsAsync()
    {
        ErrorMessage = string.Empty;
        try
        {
            using var db = DatabaseInitializer.CreateDbContext();
            var d = await db.Decharges
                .Include(x => x.Employee)
                .Include(x => x.Items)
                    .ThenInclude(i => i.Equipment)
                .FirstOrDefaultAsync(x => x.Id == _dechargeId);

            if (d == null)
            {
                ErrorMessage = "Décharge introuvable.";
                return;
            }

            Decharge = d;
            DechargeNumber = d.DechargeNumber;
            EmployeeId = d.EmployeeId ?? 0;
            EmployeeFullName = d.Employee?.FullName ?? "—";
            EmployeeMatricule = d.Employee?.Matricule ?? "—";
            EmployeeFunction = d.Employee?.Function ?? "—";
            EmployeeStructure = d.Employee?.Structure ?? "—";
            EmployeeRegion = d.Employee?.Region ?? "—";
            IssueDateText = d.IssueDate.ToString("dd/MM/yyyy");
            NotesText = string.IsNullOrWhiteSpace(d.Notes) ? "Aucune note" : d.Notes;

            Status = d.Status.ToUpper();
            IsActive = Status == "ACTIVE";
            IsReturned = Status == "RETOURNÉE" || Status == "COMPLETED";

            var itemDisplayModels = d.Items.Select(i => new DechargeDetailsItemModel
            {
                ItemId = i.Id,
                EquipmentId = i.EquipmentId ?? 0,
                Type = i.Equipment?.Type ?? "—",
                Brand = i.Equipment?.Brand ?? "—",
                Model = i.Equipment?.Model ?? "—",
                SerialNumber = i.Equipment?.DisplaySerialNumber ?? "—",
                InventoryNumber = i.Equipment?.DisplayInventoryNumber ?? "—",
                ShCode = i.Equipment?.DisplayShCode ?? "—",
                AssignmentDate = i.AssignmentDate != default ? i.AssignmentDate : d.IssueDate,
                ReturnDate = i.ReturnDate,
                ConditionAtAssignment = string.IsNullOrWhiteSpace(i.ConditionAtAssignment) ? "—" : i.ConditionAtAssignment,
                ConditionReturned = string.IsNullOrWhiteSpace(i.ConditionReturned) ? "—" : i.ConditionReturned,
                EquipmentStatus = i.Equipment?.Status ?? EquipmentStatus.Available
            }).ToList();

            Items = new ObservableCollection<DechargeDetailsItemModel>(itemDisplayModels);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Erreur de chargement : " + ex.Message;
        }
    }

    [RelayCommand]
    public void OpenReturnModal()
    {
        ErrorMessage = string.Empty;
        ReturnCondition = "Bon état";
        ReturnNotes = string.Empty;
        ReturnDate = DateTimeOffset.Now;
        IsReturnModalOpen = true;
    }

    [RelayCommand]
    public void SetReturnConditionPreset(string? preset)
    {
        if (!string.IsNullOrWhiteSpace(preset))
        {
            ReturnCondition = preset;
        }
    }

    [RelayCommand]
    public void CancelReturnModal()
    {
        IsReturnModalOpen = false;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    public async Task SaveReturnAsync()
    {
        ErrorMessage = string.Empty;
        try
        {
            using var db = DatabaseInitializer.CreateDbContext();
            await using var transaction = await db.Database.BeginTransactionAsync();

            var decharge = await db.Decharges
                .Include(d => d.Items)
                    .ThenInclude(i => i.Equipment)
                .FirstOrDefaultAsync(d => d.Id == _dechargeId);

            if (decharge == null)
            {
                ErrorMessage = "Décharge introuvable.";
                return;
            }

            var returnDateOnly = DateOnly.FromDateTime(ReturnDate.DateTime);
            var conditionText = ReturnCondition.Trim();
            if (!string.IsNullOrWhiteSpace(ReturnNotes))
            {
                conditionText += $" — {ReturnNotes.Trim()}";
            }

            // 1. Update every DECHARGE_ITEM (ReturnDate, ConditionReturned)
            // 2. Change every equipment status to AVAILABLE
            foreach (var item in decharge.Items)
            {
                item.ReturnDate = returnDateOnly;
                item.ConditionReturned = conditionText;

                if (item.Equipment != null)
                {
                    item.Equipment.Status = EquipmentStatus.Available;
                }
            }

            // 3. Change Décharge status to RETOURNÉE
            decharge.Status = "RETOURNÉE";

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            IsReturnModalOpen = false;
            SuccessMessage = "✓ La décharge a été retournée avec succès";

            await LoadDechargeDetailsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Impossible d'effectuer la restitution : " + ex.Message;
        }
    }

    [RelayCommand]
    public async Task PrintDechargeAsync()
    {
        if (Decharge == null) return;
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "EquipmentDechargeManager");
            Directory.CreateDirectory(tempDir);
            var fileName = $"Decharge_{Decharge.DechargeNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(tempDir, fileName);

            var pdfBytes = PdfService.GeneratePdfBytes(Decharge);
            await File.WriteAllBytesAsync(filePath, pdfBytes);
            PrintService.PrintPdf(filePath);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Erreur d'impression : " + ex.Message;
        }
    }

    [RelayCommand]
    public void GoBack()
    {
        BackRequested?.Invoke();
    }
}
