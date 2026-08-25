using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using EquipmentDechargeManager.Resources;

namespace EquipmentDechargeManager.Services;

public class LocalizationManager : INotifyPropertyChanged
{
    private static readonly Lazy<LocalizationManager> _instance = new(() => new LocalizationManager());
    public static LocalizationManager Instance => _instance.Value;

    private readonly ResourceManager _resourceManager;
    private CultureInfo _currentCulture;

    public event PropertyChangedEventHandler? PropertyChanged;

    private LocalizationManager()
    {
        _resourceManager = new ResourceManager(typeof(Strings));

        _currentCulture = new CultureInfo("fr");
        CultureInfo.CurrentCulture = _currentCulture;
        CultureInfo.CurrentUICulture = _currentCulture;
    }

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture.Name != value.Name)
            {
                _currentCulture = value;
                CultureInfo.CurrentCulture = value;
                CultureInfo.CurrentUICulture = value;

                // Persist setting
                SettingsService.SaveSettings(new AppSettings { LanguageCode = value.TwoLetterISOLanguageName });

                // Refresh all indexer bindings
                OnPropertyChanged(string.Empty);
                OnPropertyChanged(nameof(CurrentLanguageCode));
                OnPropertyChanged(nameof(IsFrench));
                OnPropertyChanged(nameof(IsEnglish));
            }
        }
    }

    public string CurrentLanguageCode => _currentCulture.TwoLetterISOLanguageName.ToUpper();
    public bool IsFrench => _currentCulture.TwoLetterISOLanguageName.Equals("fr", StringComparison.OrdinalIgnoreCase);
    public bool IsEnglish => _currentCulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase);

    public void SetCulture(string cultureCode)
    {
        CurrentCulture = new CultureInfo(cultureCode);
    }

    public string this[string key]
    {
        get
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            string? value = _resourceManager.GetString(key, _currentCulture);
            return value ?? $"[{key}]";
        }
    }

    protected virtual void OnPropertyChanged(string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
