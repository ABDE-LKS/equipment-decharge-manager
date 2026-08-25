using Avalonia.Data;
using Avalonia.Markup.Xaml;
using EquipmentDechargeManager.Services;
using System;

namespace EquipmentDechargeManager.MarkupExtensions;

public class LocalizeExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public LocalizeExtension() { }

    public LocalizeExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationManager.Instance,
            Mode = BindingMode.OneWay
        };
        return binding;
    }
}
