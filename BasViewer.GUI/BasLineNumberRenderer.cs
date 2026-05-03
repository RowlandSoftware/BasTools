using System;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;

namespace BasViewer.GUI
{
    public class BasLineNumberRenderer : IBackgroundRenderer
    {
        private readonly Func<int, string?> _getFormattedLineNumber;
        private readonly Typeface _typeface;
        private readonly double _fontSize;
        private readonly IBrush _background;
        private readonly IBrush _foreground;

        public BasLineNumberRenderer(
            Func<int, string?> getFormattedLineNumber,
            Typeface? typeface = null,
            double fontSize = 14,
            IBrush? background = null,
            IBrush? foreground = null)
        {
            _getFormattedLineNumber = getFormattedLineNumber;
            _typeface = typeface ?? new Typeface("Consolas");
            _fontSize = fontSize;
            _background = background ?? Brushes.Black;
            _foreground = foreground ?? Brushes.Gray;
        }

        public KnownLayer Layer => KnownLayer.Background;

        public void Draw(TextView textView, DrawingContext context)
        {
            if (!textView.VisualLinesValid)
                return;

            const double gutterWidth = 60;

            // Background
            context.FillRectangle(_background, new Rect(0, 0, gutterWidth, textView.Bounds.Height));

            int? lastDocLine = null;

            foreach (var visualLine in textView.VisualLines)
            {
                int docLine = visualLine.FirstDocumentLine.LineNumber;

                if (lastDocLine == docLine)
                    continue;

                lastDocLine = docLine;

                string? formatted = _getFormattedLineNumber(docLine);
                if (string.IsNullOrEmpty(formatted))
                    continue;

                double y = visualLine.GetTextLineVisualYPosition(
                    visualLine.TextLines[0],
                    VisualYPosition.TextTop);

                var ft = new Avalonia.Media.FormattedText(
                    formatted,
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    _typeface,
                    _fontSize,
                    _foreground);

                double x = gutterWidth - ft.Bounds.Width - 6;

                context.DrawText(ft, new Point(x, y));
            }
        }
    }
}
