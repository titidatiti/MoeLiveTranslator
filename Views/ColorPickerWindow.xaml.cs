using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace LiveTranslator.Views
{
    public partial class ColorPickerWindow : Window, INotifyPropertyChanged
    {
        private byte _a;
        private byte _r;
        private byte _g;
        private byte _b;
        private string _hexCode;
        private SolidColorBrush _currentBrush;
        private bool _isUpdating;

        public event PropertyChangedEventHandler PropertyChanged;

        public ColorPickerWindow(string initialColor, bool supportsAlpha = true)
        {
            InitializeComponent();
            DataContext = this;
            SupportsAlpha = supportsAlpha;
            ParseColor(initialColor);
            InitPalette();
        }

        public bool SupportsAlpha { get; private set; }
        public Visibility AlphaVisibility => SupportsAlpha ? Visibility.Visible : Visibility.Collapsed;

        public class PaletteItem
        {
            public SolidColorBrush Brush { get; set; }
            public string Hex { get; set; }
        }

        public System.Collections.Generic.List<PaletteItem> PaletteColors { get; private set; }

        private void InitPalette()
        {
            var colors = new string[]
            {
                "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#00FFFF", "#FF00FF", "#FFFFFF", "#000000", "#808080", "#C0C0C0",
                "#800000", "#808000", "#008000", "#800080", "#008080", "#000080", "#FFA500", "#A52A2A", "#FFC0CB", "#DC143C",
                "#39C5BB", "#121212", "#1E1E1E", "#444444", "#888888", "#E0E0E0", "#FF4500", "#2E8B57", "#4682B4", "#6A5ACD"
            };

            PaletteColors = new System.Collections.Generic.List<PaletteItem>();
            foreach (var c in colors)
            {
                try
                {
                    var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(c);
                    PaletteColors.Add(new PaletteItem
                    {
                        Brush = new SolidColorBrush(color),
                        Hex = c
                    });
                }
                catch { }
            }
        }

        private void PaletteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is PaletteItem item)
            {
                ParseColor(item.Hex, fromUser: true);
                e.Handled = true;
            }
        }

        private double _h;
        private double _s;
        private double _v;
        private bool _isHsvUpdating;

        public double H
        {
            get => _h;
            set { if (Math.Abs(_h - value) > 0.001) { _h = value; OnPropertyChanged(); OnPropertyChanged(nameof(HueBrush)); UpdateFromHsv(); } }
        }

        public double S
        {
            get => _s;
            set { if (Math.Abs(_s - value) > 0.001) { _s = value; OnPropertyChanged(); UpdateFromHsv(); } }
        }

        public double V
        {
            get => _v;
            set { if (Math.Abs(_v - value) > 0.001) { _v = value; OnPropertyChanged(); OnPropertyChanged(nameof(VInverse)); UpdateFromHsv(); } }
        }

        public double VInverse => 1.0 - V;

        public SolidColorBrush HueBrush => new SolidColorBrush(HsvToRgb(H, 1, 1));

        public double PointerX => S * 200; // Fixed width for square
        public double PointerY => (1 - V) * 200; // Fixed height for square

        private void UpdateFromHsv()
        {
            if (_isUpdating || _isHsvUpdating) return;
            _isHsvUpdating = true;

            var color = HsvToRgb(H, S, V);
            R = color.R;
            G = color.G;
            B = color.B;

            OnPropertyChanged(nameof(PointerX));
            OnPropertyChanged(nameof(PointerY));
            OnPropertyChanged(nameof(VInverse));

            _isHsvUpdating = false;
        }

        private void UpdateToHsv()
        {
            if (_isUpdating || _isHsvUpdating) return;
            _isHsvUpdating = true;

            var hsv = RgbToHsv(R, G, B);
            _h = hsv.H;
            _s = hsv.S;
            _v = hsv.V;

            OnPropertyChanged(nameof(H));
            OnPropertyChanged(nameof(S));
            OnPropertyChanged(nameof(V));
            OnPropertyChanged(nameof(HueBrush));
            OnPropertyChanged(nameof(PointerX));
            OnPropertyChanged(nameof(PointerY));
            OnPropertyChanged(nameof(VInverse));

            _isHsvUpdating = false;
        }

        private void ColorSquare_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && sender is FrameworkElement element)
            {
                element.CaptureMouse();
                UpdateHsvFromPoint(e.GetPosition(element), element);
                e.Handled = true; // Stop event here so parent doesn't DragMove
            }
        }

        private void ColorSquare_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && sender is FrameworkElement element && element.IsMouseCaptured)
            {
                UpdateHsvFromPoint(e.GetPosition(element), element);
                e.Handled = true;
            }
        }

        private void ColorSquare_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.IsMouseCaptured)
            {
                element.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void UpdateHsvFromPoint(System.Windows.Point p, FrameworkElement element)
        {
            double x = p.X;
            double y = p.Y;

            if (x < 0) x = 0;
            if (x > element.ActualWidth) x = element.ActualWidth;
            if (y < 0) y = 0;
            if (y > element.ActualHeight) y = element.ActualHeight;

            S = x / element.ActualWidth;
            V = 1.0 - (y / element.ActualHeight);
        }

        private System.Windows.Media.Color HsvToRgb(double h, double s, double v)
        {
            int hi = Convert.ToInt32(Math.Floor(h / 60)) % 6;
            double f = h / 60 - Math.Floor(h / 60);

            v = v * 255;
            byte vByte = Convert.ToByte(v);
            byte p = Convert.ToByte(v * (1 - s));
            byte q = Convert.ToByte(v * (1 - f * s));
            byte t = Convert.ToByte(v * (1 - (1 - f) * s));

            if (hi == 0) return System.Windows.Media.Color.FromArgb(255, vByte, t, p);
            else if (hi == 1) return System.Windows.Media.Color.FromArgb(255, q, vByte, p);
            else if (hi == 2) return System.Windows.Media.Color.FromArgb(255, p, vByte, t);
            else if (hi == 3) return System.Windows.Media.Color.FromArgb(255, p, q, vByte);
            else if (hi == 4) return System.Windows.Media.Color.FromArgb(255, t, p, vByte);
            else return System.Windows.Media.Color.FromArgb(255, vByte, p, q);
        }

        private (double H, double S, double V) RgbToHsv(byte r, byte g, byte b)
        {
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));

            double h = 0;
            if (max == min) h = 0;
            else if (max == r) h = (60 * (g - b) / (max - min) + 360) % 360;
            else if (max == g) h = 60 * (b - r) / (max - min) + 120;
            else if (max == b) h = 60 * (r - g) / (max - min) + 240;

            double s = (max == 0) ? 0 : (1d - (1d * min / max));
            double v = max / 255d;

            return (h, s, v);
        }

        public byte A
        {
            get => _a;
            set { if (_a != value) { _a = value; OnPropertyChanged(); UpdateHex(); UpdateBrush(); } }
        }

        public byte R
        {
            get => _r;
            set { if (_r != value) { _r = value; OnPropertyChanged(); UpdateHex(); UpdateBrush(); UpdateToHsv(); } }
        }

        public byte G
        {
            get => _g;
            set { if (_g != value) { _g = value; OnPropertyChanged(); UpdateHex(); UpdateBrush(); UpdateToHsv(); } }
        }

        public byte B
        {
            get => _b;
            set { if (_b != value) { _b = value; OnPropertyChanged(); UpdateHex(); UpdateBrush(); UpdateToHsv(); } }
        }

        public string HexCode
        {
            get => _hexCode;
            set
            {
                if (_hexCode != value)
                {
                    _hexCode = value;
                    OnPropertyChanged();
                    ParseColor(_hexCode, fromUser: true);
                }
            }
        }

        public SolidColorBrush CurrentBrush
        {
            get => _currentBrush;
            set { _currentBrush = value; OnPropertyChanged(); }
        }

        public event Action<string> ColorChanged;

        public string ResultColor { get; private set; }

        private void UpdateHex()
        {
            if (_isUpdating) return;
            _isUpdating = true;

            if (SupportsAlpha)
            {
                HexCode = $"#{A:X2}{R:X2}{G:X2}{B:X2}";
            }
            else
            {
                // Force A to 255 just in case
                A = 255;
                HexCode = $"#{R:X2}{G:X2}{B:X2}";
            }

            ColorChanged?.Invoke(HexCode);
            _isUpdating = false;
        }

        private void UpdateBrush()
        {
            CurrentBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(A, R, G, B));
        }

        private void ParseColor(string hex, bool fromUser = false)
        {
            if (_isUpdating) return;
            if (string.IsNullOrEmpty(hex)) return;

            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
                _isUpdating = true;

                if (SupportsAlpha)
                {
                    A = color.A;
                }
                else
                {
                    A = 255;
                }

                R = color.R;
                G = color.G;
                B = color.B;
                UpdateBrush();
                UpdateToHsv();

                if (!fromUser)
                {
                    // If programmatic set, sync the hex string to be normalized
                    if (SupportsAlpha)
                        _hexCode = $"#{A:X2}{R:X2}{G:X2}{B:X2}";
                    else
                        _hexCode = $"#{R:X2}{G:X2}{B:X2}";

                    OnPropertyChanged(nameof(HexCode));
                }
                else
                {
                    // If user typed hex, notify
                    if (SupportsAlpha)
                        ColorChanged?.Invoke($"#{A:X2}{R:X2}{G:X2}{B:X2}");
                    else
                        ColorChanged?.Invoke($"#{R:X2}{G:X2}{B:X2}");
                }

                _isUpdating = false;
            }
            catch
            {
                // Invalid hex, ignore
            }
        }

        private void HueSlider_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                element.CaptureMouse();
                UpdateHueFromPoint(e.GetPosition(element), element);
                e.Handled = true;
            }
        }

        private void HueSlider_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is FrameworkElement element && element.IsMouseCaptured)
            {
                UpdateHueFromPoint(e.GetPosition(element), element);
                e.Handled = true;
            }
        }

        private void HueSlider_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.IsMouseCaptured)
            {
                element.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void UpdateHueFromPoint(System.Windows.Point p, FrameworkElement element)
        {
            double y = p.Y;
            if (y < 0) y = 0;
            if (y > element.ActualHeight) y = element.ActualHeight;

            // Gradient goes from Red (0) at Top to Red (360) at Bottom
            double ratio = y / element.ActualHeight;
            H = ratio * 360;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (SupportsAlpha)
                ResultColor = $"#{A:X2}{R:X2}{G:X2}{B:X2}";
            else
                ResultColor = $"#{R:X2}{G:X2}{B:X2}";

            DialogResult = true;
            Close();
        }

        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
