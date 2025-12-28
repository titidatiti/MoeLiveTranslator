using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;

namespace LiveTranslator.Controls
{
    public class OutlinedTextBlock : FrameworkElement
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
            "Fill", typeof(System.Windows.Media.Brush), typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(System.Windows.Media.Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            "Stroke", typeof(System.Windows.Media.Brush), typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(System.Windows.Media.Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
            "StrokeThickness", typeof(double), typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty FontFamilyProperty = TextBlock.FontFamilyProperty.AddOwner(
            typeof(OutlinedTextBlock), new FrameworkPropertyMetadata(System.Windows.SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty FontSizeProperty = TextBlock.FontSizeProperty.AddOwner(
            typeof(OutlinedTextBlock), new FrameworkPropertyMetadata(System.Windows.SystemFonts.MessageFontSize, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty FontWeightProperty = TextBlock.FontWeightProperty.AddOwner(
            typeof(OutlinedTextBlock), new FrameworkPropertyMetadata(System.Windows.SystemFonts.MessageFontWeight, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty FontStyleProperty = TextBlock.FontStyleProperty.AddOwner(
            typeof(OutlinedTextBlock), new FrameworkPropertyMetadata(System.Windows.SystemFonts.MessageFontStyle, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty TextWrappingProperty = TextBlock.TextWrappingProperty.AddOwner(
             typeof(OutlinedTextBlock), new FrameworkPropertyMetadata(TextWrapping.NoWrap, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty TextAlignmentProperty = TextBlock.TextAlignmentProperty.AddOwner(
             typeof(OutlinedTextBlock), new FrameworkPropertyMetadata(TextAlignment.Left, FrameworkPropertyMetadataOptions.AffectsRender));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public System.Windows.Media.Brush Fill
        {
            get => (System.Windows.Media.Brush)GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public System.Windows.Media.Brush Stroke
        {
            get => (System.Windows.Media.Brush)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public System.Windows.Media.FontFamily FontFamily
        {
            get => (System.Windows.Media.FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public System.Windows.FontWeight FontWeight
        {
            get => (System.Windows.FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public System.Windows.FontStyle FontStyle
        {
            get => (System.Windows.FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        private FormattedText _formattedText;

        private void EnsureFormattedText()
        {
            if (string.IsNullOrEmpty(Text)) return;

            _formattedText = new FormattedText(
                Text,
                CultureInfo.CurrentUICulture,
                System.Windows.FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretches.Normal),
                FontSize,
                System.Windows.Media.Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            // Use a large value if ActualWidth is not yet determined to prevent flickering/hiding
            _formattedText.MaxTextWidth = ActualWidth > 0 ? ActualWidth : 10000;
            _formattedText.MaxTextHeight = ActualHeight > 0 ? ActualHeight : 10000;

            if (TextWrapping == TextWrapping.Wrap && ActualWidth > 0)
            {
                _formattedText.MaxTextWidth = ActualWidth;
            }

            _formattedText.TextAlignment = TextAlignment;
        }

        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
        {
            if (string.IsNullOrEmpty(Text)) return new System.Windows.Size(0, 0);

            var ft = new FormattedText(
                Text,
                CultureInfo.CurrentUICulture,
                System.Windows.FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretches.Normal),
                FontSize,
                System.Windows.Media.Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            if (TextWrapping == TextWrapping.Wrap && !double.IsInfinity(availableSize.Width))
            {
                ft.MaxTextWidth = Math.Max(0.0001, availableSize.Width);
            }

            return new System.Windows.Size(
                Math.Min(availableSize.Width, ft.Width + StrokeThickness * 2),
                Math.Min(availableSize.Height, ft.Height + StrokeThickness * 2));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (string.IsNullOrEmpty(Text)) return;

            EnsureFormattedText();

            var origin = new System.Windows.Point(StrokeThickness, StrokeThickness);

            // Draw Stroke (Behind)
            if (Stroke != null && StrokeThickness > 0)
            {
                var geometry = _formattedText.BuildGeometry(origin);
                var pen = new System.Windows.Media.Pen(Stroke, StrokeThickness)
                {
                    LineJoin = System.Windows.Media.PenLineJoin.Round
                };
                drawingContext.DrawGeometry(null, pen, geometry);
            }

            // Draw Fill (Foreground) - Always draw this on top
            _formattedText.SetForegroundBrush(Fill);
            drawingContext.DrawText(_formattedText, origin);
        }
    }
}
