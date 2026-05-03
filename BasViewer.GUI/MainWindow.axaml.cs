using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using BasTools.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace BasViewer.GUI
{
    public partial class MainWindow : Window
    {
        ProgInfo? progInfo;
        FormattingOptions? formatOptions;
        BasToolsEngine? engine;
        private List<DisplayLine> _displayLines = new();
        public MainWindow(string[] args)
        {
            InitializeComponent();

            if (Design.IsDesignMode)
                return;

            bool flgZ80 = false;
            engine = new BasToolsEngine();
            progInfo = new ProgInfo(flgZ80, false, "");
            formatOptions = new FormattingOptions(true);

            this.AddHandler(DragDrop.DragOverEvent, DragOver);
            this.AddHandler(DragDrop.DropEvent, Drop);
            // Add custom gutter renderer
            Editor.TextArea.TextView.BackgroundRenderers.Add(
                new BasLineNumberRenderer(GetFormattedLineNumber));

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

                    LoadDisplayLines(displayList);
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

        internal void LoadDisplayLines(List<DisplayLine> lines)
        {
            _displayLines = lines;
            Editor.Text = string.Join("\n", lines.Select(l => l.LineBody));
            Editor.TextArea.TextView.InvalidateVisual();
        }
        /*private void LoadDisplayLines(List<DisplayLine> lines)
        {
            // Populate the editor body
            Editor.Text = string.Join("\n", lines.Select(l => l.LineBody));

            // Populate the gutter using your formatted numbers
            LineNumbers.Text = string.Join("\n", lines.Select(l => l.FormattedLineNumber));
        }*/

        public void LoadFile(string path)
        {
            if (File.Exists(path))
            {
                Editor.Text = File.ReadAllText(path);
            }
            else
            {
                Editor.Text = $"File not found:\n{path}";
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
}