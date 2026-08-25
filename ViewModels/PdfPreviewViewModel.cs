using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentDechargeManager.Data.Entities;
using EquipmentDechargeManager.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EquipmentDechargeManager.ViewModels;

public partial class PdfPreviewViewModel : ViewModelBase
{
    private readonly Decharge _decharge;

    [ObservableProperty]
    private string _dechargeNumber = string.Empty;

    [ObservableProperty]
    private string _employeeFullName = string.Empty;

    [ObservableProperty]
    private int _itemCount;

    [ObservableProperty]
    private string _pdfFilePath = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public Action? CloseRequested { get; set; }
    public Func<string, Task<string?>>? SaveFilePickerRequested { get; set; }

    public PdfPreviewViewModel(Decharge decharge)
    {
        _decharge = decharge;
        DechargeNumber = decharge.DechargeNumber;
        EmployeeFullName = decharge.Employee?.FullName ?? "N/A";
        ItemCount = decharge.Items?.Count ?? 0;

        GenerateTempPdf();
    }

    private void GenerateTempPdf()
    {
        try
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "EquipmentDechargeManager");
            Directory.CreateDirectory(tempDir);

            string fileName = $"Decharge_{_decharge.DechargeNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            PdfFilePath = Path.Combine(tempDir, fileName);

            byte[] pdfBytes = PdfService.GeneratePdfBytes(_decharge);
            File.WriteAllBytes(PdfFilePath, pdfBytes);

            StatusMessage = "PDF généré avec succès.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erreur lors de la génération: {ex.Message}";
        }
    }

    [RelayCommand]
    public void Print()
    {
        if (File.Exists(PdfFilePath))
        {
            PrintService.PrintPdf(PdfFilePath);
            StatusMessage = "Impression lancée...";
        }
    }

    [RelayCommand]
    public void OpenPdf()
    {
        if (File.Exists(PdfFilePath))
        {
            PrintService.OpenPdf(PdfFilePath);
        }
    }

    [RelayCommand]
    public async Task SaveAsAsync()
    {
        if (!File.Exists(PdfFilePath)) return;

        if (SaveFilePickerRequested != null)
        {
            string defaultName = $"Decharge_{_decharge.DechargeNumber}.pdf";
            string? destinationPath = await SaveFilePickerRequested.Invoke(defaultName);

            if (!string.IsNullOrEmpty(destinationPath))
            {
                File.Copy(PdfFilePath, destinationPath, overwrite: true);
                StatusMessage = "PDF enregistré avec succès.";
            }
        }
    }

    [RelayCommand]
    public void Close()
    {
        CloseRequested?.Invoke();
    }
}
