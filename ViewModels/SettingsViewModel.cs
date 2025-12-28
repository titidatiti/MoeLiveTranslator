using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.IO;
using LiveTranslator.Models;
using LiveTranslator.Services;

namespace LiveTranslator.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private AppSettings _settings;
        private AudioService _audioService; // Just for getting devices

        public List<string> Devices { get; private set; }
        public int SelectedDeviceIndex { get; set; }

        public string SourceLanguage { get; set; }
        public string TargetLanguage { get; set; }
        public string ApiKey { get; set; }
        public string DeepLApiKey { get; set; }
        public string TranslationProvider { get; set; }

        public bool IsRestartRequired { get; private set; }
        private string _initialSourceLang;
        private string _initialTargetLang;
        private string _initialModel;
        private int _initialDevice;


        private int _noiseGateThreshold;
        public int NoiseGateThreshold
        {
            get => _noiseGateThreshold;
            set
            {
                _noiseGateThreshold = value;
                OnPropertyChanged();
            }
        }

        // Visual Settings Properties
        // Helper for Brush Binding
        private System.Windows.Media.Brush GetBrush(string hex)
        {
            try
            {
                if (string.IsNullOrEmpty(hex)) return System.Windows.Media.Brushes.Transparent;
                var c = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
                return new System.Windows.Media.SolidColorBrush(c);
            }
            catch { return System.Windows.Media.Brushes.Transparent; }
        }

        // Visual Settings Properties
        private string _themeColor;
        public string ThemeColor
        {
            get => _themeColor;
            set
            {
                _themeColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ThemeBrush));
            }
        }
        public System.Windows.Media.Brush ThemeBrush => GetBrush(ThemeColor);

        private double _windowOpacity;
        public double WindowOpacity { get => _windowOpacity; set { _windowOpacity = value; OnPropertyChanged(); } }

        private string _subtitleBackgroundColor;
        public string SubtitleBackgroundColor
        {
            get => _subtitleBackgroundColor;
            set
            {
                _subtitleBackgroundColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SubtitleBackgroundBrush));
            }
        }
        public System.Windows.Media.Brush SubtitleBackgroundBrush => GetBrush(SubtitleBackgroundColor);

        private double _subtitleOpacity;
        public double SubtitleOpacity { get => _subtitleOpacity; set { _subtitleOpacity = value; OnPropertyChanged(); } }

        private double _subtitleSpacing;
        public double SubtitleSpacing { get => _subtitleSpacing; set { _subtitleSpacing = value; OnPropertyChanged(); } }

        private string _originalTextColor;
        public string OriginalTextColor
        {
            get => _originalTextColor;
            set
            {
                _originalTextColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OriginalTextBrush));
            }
        }
        public System.Windows.Media.Brush OriginalTextBrush => GetBrush(OriginalTextColor);

        private string _subtitleShadowColor;
        public string SubtitleShadowColor
        {
            get => _subtitleShadowColor;
            set
            {
                _subtitleShadowColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SubtitleShadowBrush));
            }
        }
        public System.Windows.Media.Brush SubtitleShadowBrush => GetBrush(SubtitleShadowColor);

        private double _subtitleShadowBlur;
        public double SubtitleShadowBlur { get => _subtitleShadowBlur; set { _subtitleShadowBlur = value; OnPropertyChanged(); } }

        private double _subtitleShadowDirection;
        public double SubtitleShadowDirection { get => _subtitleShadowDirection; set { _subtitleShadowDirection = value; OnPropertyChanged(); } }

        private double _subtitleShadowDepth;
        public double SubtitleShadowDepth { get => _subtitleShadowDepth; set { _subtitleShadowDepth = value; OnPropertyChanged(); } }

        private string _subtitleStrokeColor;
        public string SubtitleStrokeColor
        {
            get => _subtitleStrokeColor;
            set
            {
                _subtitleStrokeColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SubtitleStrokeBrush));
            }
        }
        public System.Windows.Media.Brush SubtitleStrokeBrush => GetBrush(SubtitleStrokeColor);

        private double _subtitleStrokeThickness;
        public double SubtitleStrokeThickness { get => _subtitleStrokeThickness; set { _subtitleStrokeThickness = value; OnPropertyChanged(); } }

        private double _originalFontSize;
        public double OriginalFontSize { get => _originalFontSize; set { _originalFontSize = value; OnPropertyChanged(); } }

        private double _translatedFontSize;
        public double TranslatedFontSize { get => _translatedFontSize; set { _translatedFontSize = value; OnPropertyChanged(); } }

        private string _translatedTextColor;
        public string TranslatedTextColor
        {
            get => _translatedTextColor;
            set
            {
                _translatedTextColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TranslatedTextBrush));
            }
        }
        public System.Windows.Media.Brush TranslatedTextBrush => GetBrush(TranslatedTextColor);

        private string _originalFontFamily;
        public string OriginalFontFamily { get => _originalFontFamily; set { _originalFontFamily = value; OnPropertyChanged(); } }

        private bool _originalFontBold;
        public bool OriginalFontBold { get => _originalFontBold; set { _originalFontBold = value; OnPropertyChanged(); } }

        private string _translatedFontFamily;
        public string TranslatedFontFamily { get => _translatedFontFamily; set { _translatedFontFamily = value; OnPropertyChanged(); } }

        private bool _translatedFontBold;
        public bool TranslatedFontBold { get => _translatedFontBold; set { _translatedFontBold = value; OnPropertyChanged(); } }

        public List<string> SystemFonts { get; } = System.Windows.Media.Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(s => s).ToList();


        public List<string> TranslationProviders { get; } = new List<string> { "Google", "DeepL" };

        private string _selectedTranslationProvider;
        public string SelectedTranslationProvider
        {
            get => _selectedTranslationProvider;
            set
            {
                _selectedTranslationProvider = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsGoogleSelected));
                OnPropertyChanged(nameof(IsDeepLSelected));
            }
        }


        public bool IsGoogleSelected => SelectedTranslationProvider == "Google";
        public bool IsDeepLSelected => SelectedTranslationProvider == "DeepL";

        public List<string> WhisperModels { get; private set; }
        public string SelectedWhisperModel { get; set; }


        public List<LanguageOption> SupportedLanguages { get; } = new List<LanguageOption>
        {
            new LanguageOption { Name = "Japanese (ja)", Code = "ja" },
            new LanguageOption { Name = "English (en-US)", Code = "en-US" },
            new LanguageOption { Name = "Chinese Simp (zh-CN)", Code = "zh-CN" },
            new LanguageOption { Name = "Chinese Trad (zh-TW)", Code = "zh-TW" },
            new LanguageOption { Name = "Korean (ko)", Code = "ko" },
            new LanguageOption { Name = "Russian (ru)", Code = "ru" },
            new LanguageOption { Name = "French (fr)", Code = "fr" },
            new LanguageOption { Name = "German (de)", Code = "de" },
            new LanguageOption { Name = "Spanish (es)", Code = "es" }
        };

        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand OpenPromptsConfigCommand { get; }

        public RelayCommand OpenGitHubCommand { get; }
        public RelayCommand PickTranslatedTextColorCommand { get; }
        public RelayCommand RestoreDefaultsCommand { get; }

        public event System.Action RequestClose;

        public RelayCommand PickThemeColorCommand { get; }
        public RelayCommand PickSubtitleColorCommand { get; }
        public RelayCommand PickOriginalColorCommand { get; }
        public RelayCommand PickShadowColorCommand { get; }
        public RelayCommand PickStrokeColorCommand { get; }

        public event System.Action PreviewRequested;

        private void HookPreview()
        {
            this.PropertyChanged += (s, e) =>
            {
                // Filter relevant properties to avoid loops or unnecessary updates? 
                // For now, any change triggers preview.
                PreviewRequested?.Invoke();
            };
        }

        public SettingsViewModel(AppSettings settings)
        {
            _settings = settings;
            _audioService = new AudioService();
            // ... (keep existing initialization)
            Devices = _audioService.GetInputDevices();
            WhisperModels = _audioService.GetWhisperModels();
            _audioService.Dispose();

            // Init properties
            SelectedDeviceIndex = _settings.InputDeviceIndex;
            SourceLanguage = _settings.SourceLanguage;
            TargetLanguage = _settings.TargetLanguage;
            ApiKey = _settings.GoogleApiKey;
            DeepLApiKey = _settings.DeepLApiKey;
            TranslationProvider = _settings.TranslationProvider ?? "Google";

            NoiseGateThreshold = _settings.NoiseGateThreshold;
            SelectedTranslationProvider = _settings.TranslationProvider ?? "Google";
            SelectedWhisperModel = _settings.WhisperModel;

            // Visual Init
            ThemeColor = _settings.ThemeColor ?? "#00FFC3";
            WindowOpacity = _settings.WindowOpacity;
            SubtitleBackgroundColor = _settings.SubtitleBackgroundColor ?? "#000000";
            SubtitleOpacity = _settings.SubtitleOpacity;
            SubtitleSpacing = _settings.SubtitleSpacing;

            OriginalTextColor = _settings.OriginalTextColor ?? "#D5D5D5";
            TranslatedTextColor = _settings.TranslatedTextColor ?? "#00FFC3"; // Default to old theme behavior

            SubtitleShadowColor = _settings.SubtitleShadowColor ?? "#000000";
            SubtitleShadowBlur = _settings.SubtitleShadowBlur;
            SubtitleShadowDirection = _settings.SubtitleShadowDirection;
            SubtitleShadowDepth = _settings.SubtitleShadowDepth;
            SubtitleStrokeColor = _settings.SubtitleStrokeColor ?? "#000000";
            SubtitleStrokeThickness = _settings.SubtitleStrokeThickness;

            OriginalFontSize = _settings.OriginalFontSize;
            TranslatedFontSize = _settings.TranslatedFontSize;
            OriginalFontFamily = _settings.OriginalFontFamily ?? "Segoe UI";
            OriginalFontBold = _settings.OriginalFontBold;
            TranslatedFontFamily = _settings.TranslatedFontFamily ?? "Segoe UI";
            TranslatedFontBold = _settings.TranslatedFontBold;

            if (WhisperModels.Count > 0 && (string.IsNullOrEmpty(SelectedWhisperModel) || !WhisperModels.Contains(SelectedWhisperModel)))
            {
                SelectedWhisperModel = WhisperModels[0];
            }

            // Validate index
            if (SelectedDeviceIndex >= Devices.Count) SelectedDeviceIndex = 0;

            _initialSourceLang = SourceLanguage;
            _initialTargetLang = TargetLanguage;
            _initialModel = SelectedWhisperModel;
            _initialDevice = SelectedDeviceIndex;

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            OpenPromptsConfigCommand = new RelayCommand(OpenPromptsConfig);
            OpenGitHubCommand = new RelayCommand(OpenGitHub);

            PickThemeColorCommand = new RelayCommand((o) => ShowColorDialog(ThemeColor, (c) => ThemeColor = c, false)); // No Alpha
            PickSubtitleColorCommand = new RelayCommand((o) => ShowColorDialog(SubtitleBackgroundColor, (c) => SubtitleBackgroundColor = c, false)); // No Alpha

            PickOriginalColorCommand = new RelayCommand((o) => ShowColorDialog(OriginalTextColor, (c) => OriginalTextColor = c, true));
            PickTranslatedTextColorCommand = new RelayCommand((o) => ShowColorDialog(TranslatedTextColor, (c) => TranslatedTextColor = c, true)); // New Command

            PickShadowColorCommand = new RelayCommand((o) => ShowColorDialog(SubtitleShadowColor, (c) => SubtitleShadowColor = c, true));
            PickStrokeColorCommand = new RelayCommand((o) => ShowColorDialog(SubtitleStrokeColor, (c) => SubtitleStrokeColor = c, true));
            RestoreDefaultsCommand = new RelayCommand(RestoreDefaults);

            HookPreview();
        }

        private void PickThemeColor(object obj)
        {
            ShowColorDialog(ThemeColor, (c) => ThemeColor = c, false);
        }

        private void PickSubtitleColor(object obj)
        {
            ShowColorDialog(SubtitleBackgroundColor, (c) => SubtitleBackgroundColor = c, false);
        }

        private void ShowColorDialog(string currentHex, System.Action<string> onUpdate, bool supportsAlpha)
        {
            try
            {
                var dialog = new LiveTranslator.Views.ColorPickerWindow(currentHex, supportsAlpha);
                if (System.Windows.Application.Current.MainWindow != null)
                {
                    dialog.Owner = System.Windows.Application.Current.MainWindow;
                }

                // Live Update
                dialog.ColorChanged += (newColor) =>
                {
                    onUpdate?.Invoke(newColor);
                };

                if (dialog.ShowDialog() == true)
                {
                    onUpdate?.Invoke(dialog.ResultColor);
                }
                else
                {
                    // Cancelled, revert
                    onUpdate?.Invoke(currentHex);
                }
            }
            catch
            {
                // Fallback
            }
        }

        private void OpenGitHub(object obj)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/titidatiti/MoeLiveTranslator",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void OpenPromptsConfig(object obj)
        {
            try
            {
                var path = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "prompts.json");
                if (!File.Exists(path))
                {
                    // Create default if missing (handled by my tool earlier, but good safety)
                }
                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error opening config: {ex.Message}");
            }
        }

        private void Save(object obj)
        {
            _settings.InputDeviceIndex = SelectedDeviceIndex;
            _settings.SourceLanguage = SourceLanguage;
            _settings.TargetLanguage = TargetLanguage;
            _settings.GoogleApiKey = ApiKey;

            _settings.NoiseGateThreshold = NoiseGateThreshold;
            // _settings.Provider = SelectedProvider; // Removed
            _settings.TranslationProvider = SelectedTranslationProvider;
            _settings.DeepLApiKey = DeepLApiKey;
            _settings.WhisperModel = SelectedWhisperModel;

            // Visual Save
            _settings.ThemeColor = ThemeColor;
            _settings.WindowOpacity = WindowOpacity;
            _settings.SubtitleBackgroundColor = SubtitleBackgroundColor;
            _settings.SubtitleOpacity = SubtitleOpacity;
            _settings.SubtitleSpacing = SubtitleSpacing;

            _settings.OriginalTextColor = OriginalTextColor;
            _settings.TranslatedTextColor = TranslatedTextColor; // Save new prop

            _settings.SubtitleShadowColor = SubtitleShadowColor;
            _settings.SubtitleShadowBlur = SubtitleShadowBlur;
            _settings.SubtitleShadowDirection = SubtitleShadowDirection;
            _settings.SubtitleShadowDepth = SubtitleShadowDepth;
            _settings.SubtitleStrokeColor = SubtitleStrokeColor;
            _settings.SubtitleStrokeThickness = SubtitleStrokeThickness;

            _settings.OriginalFontSize = OriginalFontSize;
            _settings.TranslatedFontSize = TranslatedFontSize;
            _settings.OriginalFontFamily = OriginalFontFamily;
            _settings.OriginalFontBold = OriginalFontBold;
            _settings.TranslatedFontFamily = TranslatedFontFamily;
            _settings.TranslatedFontBold = TranslatedFontBold;

            if (_initialSourceLang != SourceLanguage ||
                _initialTargetLang != TargetLanguage ||
                _initialModel != SelectedWhisperModel ||
                _initialDevice != SelectedDeviceIndex)
            {
                IsRestartRequired = true;
            }

            RequestClose?.Invoke();
        }

        private void Cancel(object obj)
        {
            RequestClose?.Invoke();
        }

        private void RestoreDefaults(object obj)
        {
            if (System.Windows.MessageBox.Show("Are you sure you want to restore all settings to default?", "Restore Defaults", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            var defaults = new AppSettings();

            // Reset properties
            SelectedDeviceIndex = defaults.InputDeviceIndex;
            SourceLanguage = defaults.SourceLanguage;
            TargetLanguage = defaults.TargetLanguage;
            // We usually DON'T reset API keys for safety/convenience, 
            // but the user asked for "Restore Defaults". 
            // Let's reset everything except API keys to be safe.
            // ApiKey = defaults.GoogleApiKey;
            // DeepLApiKey = defaults.DeepLApiKey;

            NoiseGateThreshold = defaults.NoiseGateThreshold;
            SelectedTranslationProvider = defaults.TranslationProvider ?? "Google";
            SelectedWhisperModel = defaults.WhisperModel;

            ThemeColor = defaults.ThemeColor;
            WindowOpacity = defaults.WindowOpacity;
            SubtitleBackgroundColor = defaults.SubtitleBackgroundColor;
            SubtitleOpacity = defaults.SubtitleOpacity;
            SubtitleSpacing = defaults.SubtitleSpacing;

            OriginalTextColor = defaults.OriginalTextColor;
            TranslatedTextColor = defaults.TranslatedTextColor;

            SubtitleShadowColor = defaults.SubtitleShadowColor;
            SubtitleShadowBlur = defaults.SubtitleShadowBlur;
            SubtitleShadowDirection = defaults.SubtitleShadowDirection;
            SubtitleShadowDepth = defaults.SubtitleShadowDepth;
            SubtitleStrokeColor = defaults.SubtitleStrokeColor;
            SubtitleStrokeThickness = defaults.SubtitleStrokeThickness;

            OriginalFontSize = defaults.OriginalFontSize;
            TranslatedFontSize = defaults.TranslatedFontSize;
            OriginalFontFamily = defaults.OriginalFontFamily;
            OriginalFontBold = defaults.OriginalFontBold;
            TranslatedFontFamily = defaults.TranslatedFontFamily;
            TranslatedFontBold = defaults.TranslatedFontBold;

            // Trigger preview for all components
            OnPropertyChanged(""); // Update all bindings
        }
    }

    public class LanguageOption
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }
}
