using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using EquipmentDechargeManager.ViewModels;

namespace EquipmentDechargeManager;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var type = param.GetType();
        var fullName = type.FullName;
        if (string.IsNullOrWhiteSpace(fullName))
            return null;

        // Convert ViewModelName to ViewName by replacing namespace and "ViewModel" suffix
        var viewTypeName = fullName
            .Replace("EquipmentDechargeManager.ViewModels", "EquipmentDechargeManager.Views", StringComparison.Ordinal)
            .Replace("ViewModel", "View", StringComparison.Ordinal);

        var viewType = Type.GetType(viewTypeName, throwOnError: false);
        if (viewType is not null)
        {
            return (Control)Activator.CreateInstance(viewType)!;
        }

        return new TextBlock { Text = "Not Found: " + viewTypeName };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
