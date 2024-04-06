using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

// This code is very slightly modified from the WPF Text Outline Font class by Clifford Nelson, found at https://www.codeproject.com/script/Articles/ViewDownloads.aspx?aid=1106695
// Its not absolutely necessary for this screensaver. However, it does provide and outline text font that is more readable when placed above an image.
// If you don't want to use it, just replace the TextPath object in MainWindow.xaml with a TextBlock or equivalent
// and comments out the line in MainWindow.cs that specifies the StrokeThickness

namespace SlideShowScreenSaver
{
    // This class creates an outlined text font as a replacement for other text objects, such as a text block

    public class TextPath : Shape
    {
        private Geometry _textGeometry;

        #region Dependency Properties

        public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(
            typeof(TextPath),
            new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily,
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.Inherits,
                OnPropertyChanged));

        [Bindable(true), Category("Appearance")]
        [Localizability(LocalizationCategory.Font)]
        [TypeConverter(typeof(FontFamilyConverter))]
        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(
            typeof(TextPath),
            new FrameworkPropertyMetadata(SystemFonts.MessageFontSize,
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                OnPropertyChanged));

        [Bindable(true), Category("Appearance")]
        [TypeConverter(typeof(FontSizeConverter))]
        [Localizability(LocalizationCategory.None)]
        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public static readonly DependencyProperty FontStretchProperty = TextElement.FontStretchProperty.AddOwner(
            typeof(TextPath),
            new FrameworkPropertyMetadata(TextElement.FontStretchProperty.DefaultMetadata.DefaultValue,
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.Inherits,
                OnPropertyChanged));

        [Bindable(true), Category("Appearance")]
        [TypeConverter(typeof(FontStretchConverter))]
        public FontStretch FontStretch
        {
            get => (FontStretch)GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        public static readonly DependencyProperty FontStyleProperty = TextElement.FontStyleProperty.AddOwner(
            typeof(TextPath),
            new FrameworkPropertyMetadata(SystemFonts.MessageFontStyle,
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.Inherits,
                OnPropertyChanged));

        [Bindable(true), Category("Appearance")]
        [TypeConverter(typeof(FontStyleConverter))]
        public FontStyle FontStyle
        {
            get => (FontStyle)GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(
            typeof(TextPath),
            new FrameworkPropertyMetadata(SystemFonts.MessageFontWeight,
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.Inherits,
                OnPropertyChanged));

        [Bindable(true), Category("Appearance")]
        [TypeConverter(typeof(FontWeightConverter))]
        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public static readonly DependencyProperty OriginPointProperty =
            DependencyProperty.Register(nameof(Origin), typeof(Point), typeof(TextPath),
                new FrameworkPropertyMetadata(new Point(0, 0),
                    FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                    OnPropertyChanged));

        [Bindable(true), Category("Appearance")]
        [TypeConverter(typeof(PointConverter))]
        public Point Origin
        {
            get => (Point)GetValue(OriginPointProperty);
            set => SetValue(OriginPointProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(TextPath),
                new FrameworkPropertyMetadata(string.Empty,
                    FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                    OnPropertyChanged));

        [Bindable(true), Category("Appearance")]
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        #endregion

        protected override Geometry DefiningGeometry => _textGeometry ?? Geometry.Empty;

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((TextPath)d).CreateTextGeometry();

        private void CreateTextGeometry()
        {
            //var formattedText = new FormattedText(Text, Thread.CurrentThread.CurrentUICulture,
            //    FlowDirection.LeftToRight,
            //    new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, Brushes.Black);
            var formattedText = new FormattedText(Text, Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight,
               new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            _textGeometry = formattedText.BuildGeometry(Origin);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_textGeometry == null) CreateTextGeometry();
            if (_textGeometry?.Bounds == Rect.Empty)
            {
                return new Size(0, 0);
            }

            // return the desired size
            // ReSharper disable once PossibleNullReferenceException
            return new Size(Math.Min(availableSize.Width, _textGeometry.Bounds.Width),
                Math.Min(availableSize.Height, _textGeometry.Bounds.Height));
        }
    }
}
