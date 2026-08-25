using Avalonia.Controls;
using EquipmentDechargeManager.ViewModels;
using System;

namespace EquipmentDechargeManager.Views;

public partial class DechargeDetailsView : UserControl
{
    public DechargeDetailsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is DechargeDetailsViewModel vm)
        {
            vm.OpenPdfPreviewRequested = (decharge) =>
            {
                var window = new PdfPreviewWindow
                {
                    DataContext = new PdfPreviewViewModel(decharge)
                };

                var parentWindow = TopLevel.GetTopLevel(this) as Window;
                if (parentWindow != null)
                {
                    window.ShowDialog(parentWindow);
                }
                else
                {
                    window.Show();
                }
            };
        }
    }
}
