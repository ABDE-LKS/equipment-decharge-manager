using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentDechargeManager.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EquipmentDechargeManager.ViewModels;

public partial class ColumnMappingItem : ObservableObject
{
    public string DbFieldKey { get; set; } = string.Empty;
    public string DbFieldLabel { get; set; } = string.Empty;
    public bool IsRequired { get; set; }

    [ObservableProperty]
    private string? _selectedSourceHeader;

    public ObservableCollection<string> AvailableSourceHeaders { get; set; } = new();
}

public partial class DataImportWizardViewModel : ViewModelBase
{
    [ObservableProperty]
    private ImportEntityType _entityType;

    [ObservableProperty]
    private int _currentStep = 1;

    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep4 => CurrentStep == 4;
    public bool IsStep6 => CurrentStep == 6;

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep4));
        OnPropertyChanged(nameof(IsStep6));
    }

    [ObservableProperty]
    private string _selectedFilePath = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private RawFileData? _rawData;

    [ObservableProperty]
    private ObservableCollection<string> _previewHeaders = new();

    [ObservableProperty]
    private ObservableCollection<List<string>> _previewRows = new();

    [ObservableProperty]
    private ObservableCollection<ColumnMappingItem> _columnMappings = new();

    [ObservableProperty]
    private ImportValidationResult? _validationResult;

    [ObservableProperty]
    private ObservableCollection<ImportRowError> _validationErrors = new();

    [ObservableProperty]
    private int _importedCount;

    [ObservableProperty]
    private int _skippedCount;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public Func<Task<string?>>? FilePickerRequested { get; set; }

    public string Title => EntityType == ImportEntityType.Employee 
        ? "Importation des Employés" 
        : "Importation des Équipements";

    public DataImportWizardViewModel(ImportEntityType entityType)
    {
        _entityType = entityType;
        CurrentStep = 1;
    }

    [RelayCommand]
    public async Task BrowseFileAsync()
    {
        ErrorMessage = string.Empty;
        if (FilePickerRequested != null)
        {
            string? filePath = await FilePickerRequested.Invoke();
            if (!string.IsNullOrEmpty(filePath))
            {
                SelectedFilePath = filePath;
                FileName = Path.GetFileName(filePath);
                await ReadFileAndPreparePreviewAsync();
            }
        }
    }

    private async Task ReadFileAndPreparePreviewAsync()
    {
        if (string.IsNullOrEmpty(SelectedFilePath)) return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            await Task.Run(() =>
            {
                RawData = ExcelCsvReader.ReadFile(SelectedFilePath);
            });

            if (RawData == null || RawData.Headers.Count == 0)
            {
                ErrorMessage = "Le fichier est vide ou les en-têtes sont introuvables.";
                IsLoading = false;
                return;
            }

            PreviewHeaders = new ObservableCollection<string>(RawData.Headers);
            PreviewRows = new ObservableCollection<List<string>>(RawData.Rows.Take(8));

            PrepareDefaultColumnMappings();
            CurrentStep = 2; // Move to Preview & Mapping
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erreur lors de la lecture du fichier : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void PrepareDefaultColumnMappings()
    {
        if (RawData == null) return;

        ColumnMappings.Clear();
        var headers = new ObservableCollection<string>(RawData.Headers);
        headers.Insert(0, string.Empty); // Empty option for unmapped

        List<(string Key, string Label, bool Required, string[] AutoMatchKeywords)> fields;

        if (EntityType == ImportEntityType.Employee)
        {
            fields = new List<(string, string, bool, string[])>
            {
                ("full_name", "Nom & Prénom", true, new[] { "nom", "fullname", "prénom", "employee", "agent" }),
                ("matricule", "Matricule", true, new[] { "matricule", "mat", "emp_id", "id" }),
                ("function", "Fonction", false, new[] { "fonction", "function", "poste", "job" }),
                ("structure", "Structure", false, new[] { "structure", "département", "dept", "service" }),
                ("region", "Région", false, new[] { "région", "region", "site", "lieu" })
            };
        }
        else
        {
            fields = new List<(string, string, bool, string[])>
            {
                ("type", "Type d'équipement", true, new[] { "type", "désignation", "designation", "categorie", "category" }),
                ("inventory_number", "N° Inventaire", true, new[] { "inventaire", "inventory", "inv", "n° inv" }),
                ("brand", "Marque", false, new[] { "marque", "brand", "fabricant", "maker" }),
                ("model", "Modèle", false, new[] { "modèle", "model" }),
                ("serial_number", "N° Série", false, new[] { "série", "serie", "serial", "sn", "n° serie" }),
                ("sh_code", "Code SH", false, new[] { "code sh", "sh", "code_sh" })
            };
        }

        foreach (var field in fields)
        {
            string? matchedHeader = RawData.Headers.FirstOrDefault(h => 
                field.AutoMatchKeywords.Any(k => h.ToLowerInvariant().Contains(k)));

            ColumnMappings.Add(new ColumnMappingItem
            {
                DbFieldKey = field.Key,
                DbFieldLabel = field.Label,
                IsRequired = field.Required,
                AvailableSourceHeaders = headers,
                SelectedSourceHeader = matchedHeader ?? string.Empty
            });
        }
    }

    [RelayCommand]
    public async Task ValidateMappingAndProceedAsync()
    {
        ErrorMessage = string.Empty;

        // Check required fields
        var unmappedRequired = ColumnMappings.Where(m => m.IsRequired && string.IsNullOrWhiteSpace(m.SelectedSourceHeader)).ToList();
        if (unmappedRequired.Any())
        {
            ErrorMessage = $"Veuillez associer les champs obligatoires : {string.Join(", ", unmappedRequired.Select(m => m.DbFieldLabel))}";
            return;
        }

        if (RawData == null) return;

        IsLoading = true;

        try
        {
            var mappingDict = ColumnMappings.ToDictionary(m => m.DbFieldKey, m => m.SelectedSourceHeader ?? string.Empty);

            if (EntityType == ImportEntityType.Employee)
            {
                ValidationResult = await DataImportService.ValidateEmployeesAsync(RawData, mappingDict);
            }
            else
            {
                ValidationResult = await DataImportService.ValidateEquipmentAsync(RawData, mappingDict);
            }

            ValidationErrors = new ObservableCollection<ImportRowError>(ValidationResult.Errors);
            CurrentStep = 4; // Move to Validation Summary
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erreur lors de la validation : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ExecuteImportAsync()
    {
        if (ValidationResult == null || ValidationResult.ValidCount == 0)
        {
            ErrorMessage = "Aucune ligne valide à importer.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            if (EntityType == ImportEntityType.Employee)
            {
                ImportedCount = await DataImportService.ExecuteImportEmployeesAsync(ValidationResult.ValidEmployeeRows);
            }
            else
            {
                ImportedCount = await DataImportService.ExecuteImportEquipmentAsync(ValidationResult.ValidEquipmentRows);
            }

            SkippedCount = ValidationResult.DuplicateCount + ValidationResult.ErrorCount;
            CurrentStep = 6; // Final confirmation
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erreur lors de l'importation en base : {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void GoBackToStep(object? parameter)
    {
        ErrorMessage = string.Empty;
        if (parameter != null && int.TryParse(parameter.ToString(), out int step))
        {
            CurrentStep = step;
        }
    }

    [RelayCommand]
    public void ResetWizard()
    {
        CurrentStep = 1;
        SelectedFilePath = string.Empty;
        FileName = string.Empty;
        RawData = null;
        PreviewHeaders.Clear();
        PreviewRows.Clear();
        ColumnMappings.Clear();
        ValidationResult = null;
        ValidationErrors.Clear();
        ErrorMessage = string.Empty;
    }
}
