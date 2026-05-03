using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;

namespace BasViewer.GUI
{
    /// <summary>
    /// Fixed-width line number margin that uses external DisplayLine data.
    /// </summary>
    public class BasLineNumberMargin : AbstractMargin
    {
        private readonly Func<int, string?> _getFormattedLineNumber;
        private readonly Typeface _typeface;
        private readonly double _fontSize;
        private readonly IBrush _background;
        private readonly IBrush _foreground;
        private const int MaxDigits = 5;
        private const double HorizontalPadding = 6.0;

        public BasLineNumberMargin(
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

        protected override Size MeasureOverride(Size availableSize)
        {
            // Fixed width: up to 5 digits + padding
            var sample = new string('9', MaxDigits);
            var ft = new FormattedText(
                sample,
                _typeface,
                _fontSize,
                TextAlignment.Right,
                TextWrapping.NoWrap,
                Size.Infinity);

            return new Size(ft.Bounds.Width + 2 * HorizontalPadding, 0);
        }

        protected override void OnRender(DrawingContext context)
        {
            var textView = TextView;
            if (textView == null || !textView.VisualLinesValid)
                return;

            var renderSize = Bounds.Size;

            // Background
            context.FillRectangle(_background, new Rect(renderSize));

            int? lastDocLine = null;

            foreach (var visualLine in textView.VisualLines)
            {
                var docLine = visualLine.FirstDocumentLine.LineNumber;

                // Only draw once per document line (wrapped lines → only first visual line gets a number)
                if (lastDocLine == docLine)
                    continue;

                lastDocLine = docLine;

                string? formatted = _getFormattedLineNumber(docLine);

                // Blank if no line number
                if (string.IsNullOrEmpty(formatted))
                    continue;

                var y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.TextTop);
                var ft = new FormattedText(
                    formatted,
                    _typeface,
                    _fontSize,
                    TextAlignment.Right,
                    TextWrapping.NoWrap,
                    Size.Infinity);

                double x = renderSize.Width - HorizontalPadding;

                context.DrawText(_foreground, new Point(x - ft.Bounds.Width, y), ft);
            }
        }
    }
}
