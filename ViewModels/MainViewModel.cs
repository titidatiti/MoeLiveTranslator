using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LiveTranslator.Models;
using LiveTranslator.Services;
using LiveTranslator.ViewModels;
using System.Threading.Tasks;

namespace LiveTranslator.ViewModels
{
    public class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly ConfigService _configService;
        private readonly AudioService _audioService;
        private readonly GoogleTranslateService _translateService;
        private readonly System.Collections.Concurrent.BlockingCollection<string> _translationQueue = new System.Collections.Concurrent.BlockingCollection<string>();

        private AppSettings _settings;
        private bool _isListening;
        private string _statusText = "Double Click to Start Listening";
        private Visibility _statusVisibility = Visibility.Visible;

        public ObservableCollection<LogItem> Logs { get; } = new ObservableCollection<LogItem>();

        public AppSettings Settings => _settings;

        // --- Visual Properties ---
        private System.Windows.Media.Brush _themeBrush;
        public System.Windows.Media.Brush ThemeBrush { get => _themeBrush; set { _themeBrush = value; OnPropertyChanged(); } }

        private double _windowOpacity;
        public double WindowOpacity { get => _windowOpacity; set { _windowOpacity = value; OnPropertyChanged(); } }

        private System.Windows.Media.Brush _subtitleBackgroundBrush;
        public System.Windows.Media.Brush SubtitleBackgroundBrush { get => _subtitleBackgroundBrush; set { _subtitleBackgroundBrush = value; OnPropertyChanged(); } }

        private Thickness _subtitleMargin;
        public Thickness SubtitleMargin { get => _subtitleMargin; set { _subtitleMargin = value; OnPropertyChanged(); } }

        private double _originalFontSize;
        public double OriginalFontSize { get => _originalFontSize; set { _originalFontSize = value; OnPropertyChanged(); } }

        private double _translatedFontSize;
        public double TranslatedFontSize { get => _translatedFontSize; set { _translatedFontSize = value; OnPropertyChanged(); } }

        private System.Windows.Media.FontFamily _originalFontFamily;
        public System.Windows.Media.FontFamily OriginalFontFamily { get => _originalFontFamily; set { _originalFontFamily = value; OnPropertyChanged(); } }

        private System.Windows.Media.FontFamily _translatedFontFamily;
        public System.Windows.Media.FontFamily TranslatedFontFamily { get => _translatedFontFamily; set { _translatedFontFamily = value; OnPropertyChanged(); } }

        private System.Windows.FontWeight _originalFontWeight;
        public System.Windows.FontWeight OriginalFontWeight { get => _originalFontWeight; set { _originalFontWeight = value; OnPropertyChanged(); } }

        private System.Windows.FontWeight _translatedFontWeight;
        public System.Windows.FontWeight TranslatedFontWeight { get => _translatedFontWeight; set { _translatedFontWeight = value; OnPropertyChanged(); } }
        // -------------------------

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public Visibility StatusVisibility
        {
            get => _statusVisibility;
            set { _statusVisibility = value; OnPropertyChanged(); }
        }

        private bool _isError;
        public bool IsError
        {
            get => _isError;
            set { _isError = value; OnPropertyChanged(); }
        }

        private string _loadingStatus = "Initializing...";
        public string LoadingStatus
        {
            get => _loadingStatus;
            set { _loadingStatus = value; OnPropertyChanged(); }
        }

        public bool IsListening
        {
            get => _isListening;
            set
            {
                _isListening = value;
                OnPropertyChanged();
                if (_isListening)
                {
                    StatusVisibility = Visibility.Collapsed;
                    IsError = false;
                }
                else
                {
                    if (!IsError)
                    {
                        StatusText = "Double Click to Start Listening";
                    }
                    StatusVisibility = Visibility.Visible;
                }
            }
        }

        public ICommand ToggleListeningCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand ExitCommand { get; }

        public MainViewModel()
        {
            _configService = new ConfigService();
            _settings = _configService.Load();

            _audioService = new AudioService();
            _audioService.OnSpeechRecognized += AudioService_OnSpeechRecognized;
            _audioService.OnError += AudioService_OnError;
            _audioService.OnListeningStatusChanged += (s, listening) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => IsListening = listening);
            };
            _audioService.OnLog += (msg) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => LoadingStatus = msg);
            };
            _audioService.OnAudioLevel += (s, level) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => AudioLevel = level);
            };
            _audioService.OnSystemMessage += (s, msg) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => AddLog("SYSTEM", msg));
            };


            _translateService = new GoogleTranslateService();

            ToggleListeningCommand = new RelayCommand(ExecuteToggleListening, (o) => !IsLoading);
            OpenSettingsCommand = new RelayCommand(ExecuteOpenSettings);
            ExitCommand = new RelayCommand(ExecuteExit);

            Task.Run(ProcessTranslationQueue);

            // Initialize Visuals
            IsTopmost = _settings.IsTopmost;
            RefreshVisualSettings();
        }

        private int _audioLevel;
        public int AudioLevel
        {
            get => _audioLevel;
            set { _audioLevel = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool _isTopmost;
        public bool IsTopmost
        {
            get => _isTopmost;
            set
            {
                _isTopmost = value;
                _settings.IsTopmost = value;
                _configService.Save(_settings);
                OnPropertyChanged();
            }
        }

        private async void ExecuteToggleListening(object obj)
        {
            if (IsLoading) return;

            IsLoading = true;
            LoadingStatus = "Initializing...";
            await Task.Delay(100);

            try
            {
                if (IsListening)
                {
                    _audioService.Stop();
                }
                else
                {
                    IsError = false;
                    StartListening();

                    int timeout = 150;
                    while (!IsListening && !IsError && timeout > 0)
                    {
                        await Task.Delay(100);
                        timeout--;
                    }
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void StartListening()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => Logs.Clear());
            _audioService.Start(_settings.InputDeviceIndex, _settings.SourceLanguage, _settings.NoiseGateThreshold, _settings.WhisperModel);
        }

        private void ExecuteOpenSettings(object obj)
        {
            OnOpenSettingsRequest?.Invoke();
        }

        public event Action OnOpenSettingsRequest;

        private void ExecuteExit(object obj)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void AudioService_OnSpeechRecognized(object sender, string text)
        {
            _translationQueue.Add(text);
        }

        private async Task ProcessTranslationQueue()
        {
            foreach (var text in _translationQueue.GetConsumingEnumerable())
            {
                try
                {
                    string translated = await _translateService.TranslateAsync(text,
                        ResolveLangCode(_settings.SourceLanguage),
                        _settings.TargetLanguage,
                        _settings.GoogleApiKey);

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        AddLog(text, translated);
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        AddLog("ERROR", $"{ex.Message} (Src: {text})");
                    });
                }
            }
        }

        private string ResolveLangCode(string cultureInfoName)
        {
            if (string.IsNullOrEmpty(cultureInfoName)) return "en";
            return cultureInfoName;
        }

        private void AudioService_OnError(object sender, string error)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                AddLog("ERROR", error);
                StatusText = $"ERROR:\n{error}";
                IsError = true;
                StatusVisibility = Visibility.Visible;
            });
        }

        private void AddLog(string original, string translated)
        {
            Logs.Add(new LogItem
            {
                Original = original,
                Translated = translated,
                IsError = original == "ERROR",
            });

            while (Logs.Count > 30)
            {
                Logs.RemoveAt(0);
            }
        }

        private System.Windows.Media.Brush _originalTextBrush;
        public System.Windows.Media.Brush OriginalTextBrush { get => _originalTextBrush; set { _originalTextBrush = value; OnPropertyChanged(); } }

        private System.Windows.Media.Brush _subtitleStrokeBrush;
        public System.Windows.Media.Brush SubtitleStrokeBrush { get => _subtitleStrokeBrush; set { _subtitleStrokeBrush = value; OnPropertyChanged(); } }

        private double _subtitleStrokeThickness;
        public double SubtitleStrokeThickness { get => _subtitleStrokeThickness; set { _subtitleStrokeThickness = value; OnPropertyChanged(); } }

        private System.Windows.Media.Brush _translatedTextBrush;
        public System.Windows.Media.Brush TranslatedTextBrush { get => _translatedTextBrush; set { _translatedTextBrush = value; OnPropertyChanged(); } }

        public void SaveSettings()
        {
            _configService.Save(_settings);
            RefreshVisualSettings();
        }

        public void RefreshVisualSettings()
        {
            ApplyVisualSettings(_settings.ThemeColor, _settings.WindowOpacity, _settings.SubtitleBackgroundColor, _settings.SubtitleOpacity,
                _settings.SubtitleSpacing, _settings.OriginalFontSize, _settings.TranslatedFontSize,
                _settings.OriginalTextColor, _settings.TranslatedTextColor, _settings.SubtitleShadowColor, _settings.SubtitleShadowBlur,
                _settings.SubtitleShadowDirection, _settings.SubtitleShadowDepth,
                _settings.SubtitleStrokeColor, _settings.SubtitleStrokeThickness, _settings.OriginalFontFamily, _settings.TranslatedFontFamily, _settings.OriginalFontBold, _settings.TranslatedFontBold);
        }

        public void PreviewVisualSettings(string themeColor, double winOpacity, string subBgColor, double subOpacity, double subSpacing,
                                          double origSize, double transSize, string origColor, string transColor, string shadowColor, double shadowBlur, double shadowDir, double shadowDepth,
                                          string strokeColor, double strokeThickness, string originalFontFamily, string translatedFontFamily, bool originalFontBold, bool translatedFontBold)
        {
            ApplyVisualSettings(themeColor, winOpacity, subBgColor, subOpacity, subSpacing, origSize, transSize, origColor, transColor, shadowColor, shadowBlur, shadowDir, shadowDepth, strokeColor, strokeThickness, originalFontFamily, translatedFontFamily, originalFontBold, translatedFontBold);
        }

        private void ApplyVisualSettings(string themeColor, double winOpacity, string subBgColor, double subOpacity, double subSpacing,
                                         double origSize, double transSize, string origTextColor, string transTextColor, string shadowColor, double shadowBlur, double shadowDir, double shadowDepth,
                                         string strokeColor, double strokeThickness, string originalFontFamily, string translatedFontFamily, bool originalFontBold, bool translatedFontBold)
        {
            OriginalFontFamily = new System.Windows.Media.FontFamily(originalFontFamily ?? "Segoe UI");
            TranslatedFontFamily = new System.Windows.Media.FontFamily(translatedFontFamily ?? "Segoe UI");

            OriginalFontWeight = originalFontBold ? FontWeights.Bold : FontWeights.Normal;
            TranslatedFontWeight = translatedFontBold ? FontWeights.Bold : FontWeights.Normal;
            // ... (keep previous logic until Shadow)
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(themeColor ?? "#00FFC3");
                ThemeBrush = new SolidColorBrush(color);
            }
            catch { ThemeBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00FFC3")); }

            WindowOpacity = winOpacity;

            try
            {
                var subColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(subBgColor ?? "#000000");
                subColor.A = (byte)(subOpacity * 255);
                SubtitleBackgroundBrush = new SolidColorBrush(subColor);
            }
            catch { SubtitleBackgroundBrush = System.Windows.Media.Brushes.Transparent; }

            SubtitleMargin = new Thickness(0, 0, 0, subSpacing);

            OriginalFontSize = origSize;
            TranslatedFontSize = transSize;

            // Original Text Color
            try
            {
                var c = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(origTextColor ?? "#D5D5D5");
                OriginalTextBrush = new SolidColorBrush(c);
            }
            catch { OriginalTextBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D5D5D5")); }

            // Translated Text Color
            try
            {
                var c = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(transTextColor ?? "#00FFC3");
                TranslatedTextBrush = new SolidColorBrush(c);
            }
            catch { TranslatedTextBrush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00FFC3")); }

            // Stroke Brush
            try
            {
                var c = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(strokeColor ?? "#000000");
                SubtitleStrokeBrush = new SolidColorBrush(c);
            }
            catch { SubtitleStrokeBrush = System.Windows.Media.Brushes.Black; }

            SubtitleStrokeThickness = strokeThickness;

            // Shadow
            if (!string.IsNullOrEmpty(shadowColor))
            {
                System.Windows.Media.Color shColor = System.Windows.Media.Colors.Transparent;
                try { shColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(shadowColor); } catch { }

                if (shColor.A > 0)
                {
                    var shadow = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = System.Windows.Media.Color.FromRgb(shColor.R, shColor.G, shColor.B),
                        Opacity = shColor.A / 255.0,
                        Direction = shadowDir,
                        ShadowDepth = shadowDepth,
                        BlurRadius = shadowBlur,
                        RenderingBias = System.Windows.Media.Effects.RenderingBias.Performance
                    };
                    SubtitleVisualEffect = shadow;
                }
                else
                {
                    SubtitleVisualEffect = null;
                }
            }
            else
            {
                SubtitleVisualEffect = null;
            }
        }

        private System.Windows.Media.Effects.Effect _subtitleVisualEffect;
        public System.Windows.Media.Effects.Effect SubtitleVisualEffect { get => _subtitleVisualEffect; set { _subtitleVisualEffect = value; OnPropertyChanged(); } }

        public void Dispose()
        {
            _translationQueue.CompleteAdding();
            _audioService.Dispose();
        }
    }
}
