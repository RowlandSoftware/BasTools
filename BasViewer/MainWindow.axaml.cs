using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using BasTools.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace BasViewer
{
    public partial class MainWindow : Window
    {
        /*ProgInfo? progInfo;
        FormattingOptions? formatOptions;
        BasToolsEngine? engine;
        private List<DisplayLine> _displayLines = new();*/
        public MainWindow()
        {
            InitializeComponent();
        }
        
    }
}
/*public MainWindow(string[] args)
        { }
            InitializeComponent();

            if (Design.IsDesignMode)
                return;

            /*bool flgZ80 = false;
            engine = new BasToolsEngine();
            progInfo = new ProgInfo(flgZ80, false, "");
            formatOptions = new FormattingOptions(true);

            this.AddHandler(DragDrop.DragOverEvent, DragOver);
            this.AddHandler(DragDrop.DropEvent, Drop);
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(new LightTheme());


            if (args != null && args.Length > 0)
            {
                string filename = args[0];
                progInfo.Filename = filename;

                loadBasicOrText(filename, engine, formatOptions, progInfo);
            }

            //Editor.Text = "Hello from BasAnalysis Viewer\n\nYour output goes here.";
            //LoadFile(@"K:\andrew\Development\VisualStudioProjects\BBCMicro\Examples\Menu0.bas");
        }
        internal void loadBasicOrText(string filename, BasToolsEngine engine, FormattingOptions formatOptions, ProgInfo progInfo)
        {
            try
            {
                Listing listing = engine.loadAndFormatFile(filename, formatOptions, progInfo);
                if (listing != null)
                {
                    //(Listing formattedListing, ListerOptions switches, ProgInfo progInfo)
                    ListerOptions listerOptions = new(formatOptions);
                    var displayList = engine.prepLinesForDisplay(listerOptions);

                    //LoadDisplayLines(displayList);
                }
            }
            catch
            {
                LoadFile(filename);
            }
        }
        private string? GetFormattedLineNumber(int docLine)
        {
            int index = docLine - 1;
            if (index < 0 || index >= _displayLines.Count)
                return null;

            var dl = _displayLines[index];
            return string.IsNullOrEmpty(dl.FormattedLineNumber)
                ? null
                : dl.FormattedLineNumber;
        }

        /*internal void LoadDisplayLines(List<DisplayLine> lines)
        {
            _displayLines = lines;
            LineNumbers.Text = string.Join("\n", _displayLines.Select(l => l.FormattedLineNumber));

            var inlines = new List<Inline>();
            foreach (var line in _displayLines)
            {
                inlines.AddRange(BuildInlines(line., line.Spans));
                inlines.Add(new LineBreak());
            }

            CodeText.Inlines = inlines;
        }
        public void LoadFile(string path)
        {
            if (File.Exists(path))
            {
                //Editor.Text = File.ReadAllText(path);
            }
            else
            {
                //Editor.Text = $"File not found:\n{path}";
            }
        }
        /*private IEnumerable<Inline> BuildInlines(string text, List<SpanInfo> spans)
        {
            int pos = 0;

            foreach (var span in spans.OrderBy(s => s.Start))
            {
                if (span.Start > pos)
                {
                    yield return new Run
                    {
                        Text = text.Substring(pos, span.Start - pos),
                        Foreground = (IBrush)Application.Current.FindResource("DefaultBrush")
                    };
                }

                var brushKey = AvaloniaBrushMap.GetBrushKey(span.Tag);

                yield return new Run
                {
                    Text = text.Substring(span.Start, span.Length),
                    Foreground = (IBrush)Application.Current.FindResource(brushKey)
                };

                pos = span.Start + span.Length;
            }

            if (pos < text.Length)
            {
                yield return new Run
                {
                    Text = text.Substring(pos),
                    Foreground = (IBrush)Application.Current.FindResource("DefaultBrush")
                };
            }
        }
        private void DragOver(object? sender, DragEventArgs e)
        {
            foreach (var item in e.DataTransfer.Items)
            {
                if (item.TryGetFile() != null)
                {
                    e.DragEffects = DragDropEffects.Copy;
                    return;
                }
            }

            e.DragEffects = DragDropEffects.None;
        }
        private async void Drop(object? sender, DragEventArgs e)
        {
            foreach (var item in e.DataTransfer.Items)
            {
                var storageItem = item.TryGetFile();
                if (storageItem != null)
                {
                    // Avalonia 12: IStorageItem.Path is a Uri
                    var path = storageItem.Path.LocalPath;

                    if (!string.IsNullOrEmpty(path))
                    {
                        LoadFile(path);
                        return;
                    }
                }
            }
        }
    }
}*/