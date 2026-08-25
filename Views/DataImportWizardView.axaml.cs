using Avalonia.Controls;
using Avalonia.Platform.Storage;
using EquipmentDechargeManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EquipmentDechargeManager.Views;

public partial class DataImportWizardView : UserControl
{
    public DataImportWizardView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is DataImportWizardViewModel vm)
        {
            vm.FilePickerRequested = OpenFilePickerAsync;
        }
    }

    private async Task<string?> OpenFilePickerAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Sélectionner un fichier de données Excel ou CSV",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Fichiers Excel et CSV (*.xlsx, *.csv)")
                {
                    Patterns = new[] { "*.xlsx", "*.csv" }
                }
            }
        });

        if (files.Count > 0)
        {
            return files[0].Path.LocalPath;
        }

        return null;
    }
}
