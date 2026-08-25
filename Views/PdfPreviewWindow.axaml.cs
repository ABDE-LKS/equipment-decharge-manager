using Avalonia.Controls;
using Avalonia.Platform.Storage;
using EquipmentDechargeManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EquipmentDechargeManager.Views;

public partial class PdfPreviewWindow : Window
{
    public PdfPreviewWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is PdfPreviewViewModel vm)
        {
            vm.CloseRequested = () => Close();
            vm.SaveFilePickerRequested = SaveFilePickerAsync;
        }
    }

    private async Task<string?> SaveFilePickerAsync(string defaultFileName)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Enregistrer la Décharge PDF",
            SuggestedFileName = defaultFileName,
            DefaultExtension = "pdf",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Fichiers PDF (*.pdf)") { Patterns = new[] { "*.pdf" } }
            }
        });

        return file?.Path.LocalPath;
    }
}
