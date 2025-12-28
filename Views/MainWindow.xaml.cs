using System.Collections.Specialized;
using System.Windows;
using LiveTranslator.ViewModels;

namespace LiveTranslator.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            _viewModel.RefreshVisualSettings();

            _viewModel.OnOpenSettingsRequest += OpenSettings;

            // Robust Auto-Scroll
            LogListView.Loaded += LogListView_Loaded;
        }

        private void LogListView_Loaded(object sender, RoutedEventArgs e)
        {
            var sv = GetScrollViewer(LogListView) as System.Windows.Controls.ScrollViewer;
            if (sv != null)
            {
                sv.ScrollChanged += ScrollViewer_ScrollChanged;
            }
        }

        private void ScrollViewer_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange > 0)
            {
                var sv = sender as System.Windows.Controls.ScrollViewer;
                if (sv == null) return;

                double oldExtent = e.ExtentHeight - e.ExtentHeightChange;
                double oldBottom = e.VerticalOffset + e.ViewportHeight;

                if (oldBottom >= oldExtent - 20)
                {
                    sv.ScrollToBottom();
                }
            }
        }

        private static System.Windows.DependencyObject GetScrollViewer(System.Windows.DependencyObject o)
        {
            if (o is System.Windows.Controls.ScrollViewer) return o;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(o, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }

        private void OpenSettings()
        {
            var settingsVm = new SettingsViewModel(_viewModel.Settings);

            // Real-time preview
            settingsVm.PreviewRequested += () =>
            {
                _viewModel.PreviewVisualSettings(
                    settingsVm.ThemeColor,
                    settingsVm.WindowOpacity,
                    settingsVm.SubtitleBackgroundColor,
                    settingsVm.SubtitleOpacity,
                    settingsVm.SubtitleSpacing,
                    settingsVm.OriginalFontSize,
                    settingsVm.TranslatedFontSize,
                    settingsVm.OriginalTextColor,
                    settingsVm.TranslatedTextColor,
                    settingsVm.SubtitleShadowColor,
                    settingsVm.SubtitleShadowBlur,
                    settingsVm.SubtitleShadowDirection,
                    settingsVm.SubtitleShadowDepth,
                    settingsVm.SubtitleStrokeColor,
                    settingsVm.SubtitleStrokeThickness,
                    settingsVm.OriginalFontFamily,
                    settingsVm.TranslatedFontFamily,
                    settingsVm.OriginalFontBold,
                    settingsVm.TranslatedFontBold
                );
            };

            var settingsWindow = new SettingsWindow(settingsVm);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();

            if (settingsVm.IsRestartRequired && _viewModel.IsListening)
            {
                if (_viewModel.ToggleListeningCommand.CanExecute(null))
                {
                    _viewModel.ToggleListeningCommand.Execute(null);
                }
            }

            // Save settings (Persist to disk and refresh)
            _viewModel.SaveSettings();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _viewModel.Dispose();
            base.OnClosed(e);
        }

        private System.Windows.Point _dragStartPoint;

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    if (_viewModel.ToggleListeningCommand.CanExecute(null))
                    {
                        _viewModel.ToggleListeningCommand.Execute(null);
                    }
                }
                else
                {
                    _dragStartPoint = e.GetPosition(this);
                }
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                var currentPoint = e.GetPosition(this);
                if (Math.Abs(currentPoint.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPoint.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    try { this.DragMove(); } catch { }
                }
            }
        }
    }
}
