using BasTools.Core;
using Microsoft.VisualBasic;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Runtime.Intrinsics.Arm;
using System.Security.Policy;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.LinkLabel;

namespace BasViewer.GUI
{
    public partial class Form1 : Form
    {
        private readonly string[] _args;
        ProgInfo? progInfo;
        FormattingOptions? formatOptions;
        BasToolsEngine? engine;
        private List<DisplayLine> _displayLines = new();
        private string _htmlClose;
        private string _script;
        private bool _loaded;
        private bool _textFile;
        public Form1(string[] args)
        {
            InitializeComponent();

            menuStripRight.BackColor = Color.LightSkyBlue;
            toolStripLeft.Renderer = new NoBorderRenderer();
            menuStripRight.Renderer = new NoBorderRenderer();

            // Goto Line clicks
            var dropDown = (ToolStripDropDownMenu)gotoLineToolStripMenuItem.DropDown;

            dropDown.MouseDown += (s, e) =>
            {
                // If click is outside the textbox bounds, treat it as "Goto"
                if (!toolStripTextBoxGoto.Bounds.Contains(e.Location))
                {
                    if (!string.IsNullOrWhiteSpace(toolStripTextBoxGoto.Text))
                        DoGotoLine(toolStripTextBoxGoto.Text);
                }
            };

            #region ZoomControl
            // --- Build status bar zoom control ---

            // Left spring label
            statusLeft = new ToolStripStatusLabel
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Right spring label
            statusRight = new ToolStripStatusLabel
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleRight
            };

            // Zoom slider
            zoomSlider = new TrackBar
            {
                Minimum = 50,
                Maximum = 200,
                TickFrequency = 25,
                Value = 100,
                AutoSize = false,
                Width = 150
            };

            // Host for slider
            zoomHost = new ToolStripControlHost(zoomSlider)
            {
                AutoSize = false,
                Width = 120
            };

            // Buttons + label
            zoomOutButton = new ToolStripButton("–");
            zoomInButton = new ToolStripButton("+");
            zoomPercentLabel = new ToolStripLabel("100%");
            zoomPercentLabel.DoubleClickEnabled = true;

            // Clear any old items and add new layout
            statusStrip1.Items.Clear();
            statusStrip1.Items.AddRange(new ToolStripItem[]
            {
                statusLeft,
                zoomOutButton,
                zoomHost,
                zoomInButton,
                zoomPercentLabel,
                statusRight
            });

            // Wiring behaviour
            zoomSlider.Scroll += (s, e) => ApplyZoom();

            zoomOutButton.Click += (s, e) =>
            {
                zoomSlider.Value = Math.Max(zoomSlider.Minimum, zoomSlider.Value - 10);
                ApplyZoom();
            };

            zoomInButton.Click += (s, e) =>
            {
                zoomSlider.Value = Math.Min(zoomSlider.Maximum, zoomSlider.Value + 10);
                ApplyZoom();
            };
            zoomPercentLabel.DoubleClick += (s, e) =>
            {
                zoomSlider.Value = 100;
                ApplyZoom();
            };
            #endregion
            // Init WebView2
            this.Shown += Form1_Shown;

            _args = args;
            bool flgZ80 = false;
            engine = new BasToolsEngine();
            progInfo = new ProgInfo(flgZ80, false, "");
            formatOptions = new FormattingOptions(true);
            //_theme = "Dark";
            _loaded = false;
            _textFile = false;
            _htmlClose = Environment.NewLine + "</table></body></html>";
            _script = Environment.NewLine + "<script> function toggleFold(name) { const rows = document.querySelectorAll('.' + name); const arrow = document.getElementById('arrow_' + name); const isClosed = (arrow.textContent === \"▶\"); rows.forEach(r => { r.style.display = isClosed ? \"\" : \"none\"; }); arrow.textContent = isClosed ? \"▼\" : \"▶\"} </script>" + Environment.NewLine;
            comboBoxTheme.SelectedIndex = 0;

            this.Text = "BBC BASIC Viewer";

            // DO NOT load files here
            // DO NOT touch WebView2 here
        }
        private void something() // after webView2 initialised
        {
            // if command-line argument, handle it
            if (_args != null && _args.Length > 0)
            {
                string? filename = _args.FirstOrDefault();
                if (!string.IsNullOrEmpty(filename))
                {
                    LoadFile(filename);
                }
            }
        }
        private void FileOpen()
        {
            string? filename = getFileOpen();
            if (filename == null)
                return;

            LoadFile(filename);
        }
        internal string? getFileOpen()
        {
            using var dlg = new OpenFileDialog();

            dlg.Title = "Open BASIC File";

            dlg.Filter =
                "BBC BASIC files (*.bbc)|*.bbc|" +
                "BASIC source files (*.bas)|*.bas|" +
                "Text files (*.txt)|*.txt|" +
                "All files (*.*)|*.*";

            dlg.FilterIndex = 4;   // Default = “Files with no extension”
            dlg.RestoreDirectory = true;

            return dlg.ShowDialog() == DialogResult.OK ? dlg.FileName : null;
        }
        internal bool loadBasicOrText(string filename, BasToolsEngine engine, FormattingOptions formatOptions, ProgInfo progInfo)
        {
            bool IsTextNotBasic = false;
            try
            {
                if (!engine.LoadAndFormatFile(filename, formatOptions, progInfo))
                {
                    IsTextNotBasic = true;
                    engine.LoadAndFormatTextFile(filename, formatOptions, progInfo);

                    // DEBUG
                    /*string whatever = "";
                    foreach (var pl in engine.CurrentListing.Lines)
                    {
                        whatever += $"{pl.FormattedLineNumber} {pl.TaggedLine}</br>" + Environment.NewLine;
                    }
                    label1.Visible = false;
                    webView2.NavigateToString (whatever);*/
                }
                //return IsTextNotBasic;
            }
            catch (BasToolsException ex)
            {
                MessageBox.Show($"{ex.Message}\n\n{ex.InnerException?.Message ?? ""}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return IsTextNotBasic;
        }
        private void TextToHtml(BasToolsEngine engine)
        {
            if (engine.CurrentListing == null) return;

            _loaded = true;
            combProcFnFinder.Items.Clear();

            bool pretty = toolStripButton2.Checked;

            string htmlHeader = "<html><head>" + Themes.GetCss(comboBoxTheme.Text) + _script + "</head>" + Environment.NewLine + "<body><table>" + Environment.NewLine;

            StringBuilder htmlDoc = new StringBuilder(htmlHeader);
            StringBuilder lineBody = new();

            bool IsDef = false;
            bool IsInDef = false;
            string id = string.Empty;
            foreach (var line in engine.CurrentListing.Lines)
            {
                IsDef = line.IsDef;

                lineBody.Clear();
                foreach (Token tok in BasToolsEngine.WalkTagged(line.TaggedLine))
                {
                    if (tok.tag == null)
                        lineBody.Append(tok.value);
                    else
                    {
                        if (IsDef && (tok.tag == SemanticTags.FunctionName || tok.tag == SemanticTags.ProcName))
                        {
                            if (tok.tag == SemanticTags.ProcName)
                                id = "proc_" + tok.value;
                            else
                                id = "fn_" + tok.value;

                            combProcFnFinder.Items.Add(line.FormattedPlain);
                        }
                        string tag = tok.tag.Substring(2, tok.tag.Length - 3); // peel off {= ... }
                        lineBody.Append($"<span class=\"{tag}\">");
                        lineBody.Append(tok.value);
                        lineBody.Append("</span>");
                    }
                }
                int totindent = 0;
                if (pretty)
                {
                    if (IsDef)
                        htmlDoc.Append($"<tr id={id} class=\"fold-header\" onclick=\"toggleFold('{id}')\"><td class=\"fold-marker\"><span id=\"arrow_{id}\" class=\"arrow-open\">▼</span></td><td id = \"line_{line.FormattedLineNumber}\" class = \"line-number\">{line.FormattedLineNumber}</td><td style=\"padding-left:{totindent.ToString()}ch\">{lineBody.ToString()}</td></tr>" + Environment.NewLine);
                    else if (IsInDef)
                        htmlDoc.Append($"<tr class=\"fold-body {id}\"><td class=\"fold-marker\"></td><td id = \"line_{line.FormattedLineNumber}\" class = \"line-number\">{line.FormattedLineNumber}</td><td style=\"padding-left:{totindent.ToString()}ch\">{lineBody.ToString()}</td></tr>" + Environment.NewLine);
                    else
                        htmlDoc.Append($"<tr><td class=\"fold-marker\"></td><td id = \"line_{line.FormattedLineNumber}\" class = \"line-number\">{line.FormattedLineNumber}</td><td style=\"padding-left:{totindent.ToString()}ch\">{lineBody.ToString()}</td></tr>" + Environment.NewLine);
                }
                else
                {
                    if (IsDef)
                        htmlDoc.Append($"<tr id={id} class=\"fold-header\" onclick=\"toggleFold('{id}')\"><td class=\"fold-marker\"><span id=\"arrow_{id}\" class=\"arrow-open\">▼</span></td><td id = \"line_{line.FormattedLineNumber}\" class = \"line-number\">{line.FormattedLineNumber}</td><td style=\"padding-left:{totindent.ToString()}ch\">{line.FormattedPlain}</td></tr>" + Environment.NewLine);
                    else if (IsInDef)
                        htmlDoc.Append($"<tr class=\"fold-body {id}\"><td class=\"fold-marker\"></td><td id = \"line_{line.FormattedLineNumber}\" class = \"line-number\">{line.FormattedLineNumber}</td><td style=\"padding-left:{totindent.ToString()}ch\">{line.FormattedPlain}</td></tr>" + Environment.NewLine);
                    else
                        htmlDoc.Append($"<tr><td class=\"fold-marker\"></td><td id = \"line_{line.FormattedLineNumber}\" class = \"line-number\">{line.FormattedLineNumber}</td><td style=\"padding-left:{totindent.ToString()}ch\">{line.FormattedPlain}</td></tr>" + Environment.NewLine);
                }
                if (IsInDef && !line.IsInDef)
                    IsInDef = false;

                if (IsDef)
                {
                    IsDef = false;
                    IsInDef = true;
                }
            }
            htmlDoc.Append(Environment.NewLine + _htmlClose);

            statusLeft.Text = $"{progInfo.ProgName}: {progInfo.NumberOfLines} lines";
            statusRight.Text = "Text file";
            if (combProcFnFinder.Items.Count > 0)
                combProcFnFinder.SelectedIndex = 0;
            webView2.NavigateToString(htmlDoc.ToString());
        }
        private void BasicToHtml(BasToolsEngine engine)
        {
            if (engine.CurrentListing == null) return;

            _loaded = true;
            combProcFnFinder.Items.Clear();

            bool splitLines = toolStripButton4.Checked;
            bool pretty = toolStripButton2.Checked;
            //MessageBox.Show($"Button status: {pretty} {splitLines}");

            ListerOptions listerOptions = new ListerOptions(true, false, splitLines, true); //bool indent, bool indentDefs, bool splitLines, bool pretty (ignored)
            List<DisplayLine> lines = engine.prepLinesForDisplay(listerOptions);

            string htmlHeader = "<html><head>" + Themes.GetCss(comboBoxTheme.Text) + _script + "</head><body><table>";

            StringBuilder htmlDoc = new StringBuilder(htmlHeader);

            bool IsDef = false;
            bool IsInDef = false;
            string id = string.Empty;

            foreach (DisplayLine line in lines)
            {
                IsDef = line.IsDef;

                StringBuilder lineBody = new();
                foreach (Token tok in BasToolsEngine.WalkTagged(line.LineBody))
                {
                    if (tok.tag == null)
                        lineBody.Append(tok.value);
                    else
                    {
                        if (IsDef && (tok.tag == SemanticTags.FunctionName || tok.tag == SemanticTags.ProcName))
                        {
                            if (tok.tag == SemanticTags.ProcName)
                                id = "proc_" + tok.value;
                            else
                                id = "fn_" + tok.value;

                            combProcFnFinder.Items.Add(line.PlainLine);
                        }
                        string tag = tok.tag.Substring(2, tok.tag.Length - 3); // peel off {= ... }
                        lineBody.Append($"<span class=\"{tag}\">");
                        lineBody.Append(tok.value);
                        lineBody.Append("</span>");
                    }
                }

                int totindent = (line.Indent + line.DefIndent) * 2;
                if (pretty)
                {
                    if (IsDef)
                        htmlDoc.Append($"<tr id={id} class=\"fold-header\" onclick=\"toggleFold('{id}')\"><td class=\"fold-marker\"><span id=\"arrow_{id}\" class=\"arrow-open\">▼</span></td><td id = \"line_{line.sLineNumber}\" class = \"line-number\">{line.sLineNumber}</td><td style=\"padding-left:{totindent.ToString()}ch\">{lineBody.ToString()}</td></tr>" + Environment.NewLine);
                    else if (IsInDef)
                        htmlDoc.Append($"<tr class=\"fold-body {id}\"><td class=\"fold-marker\"></td><td id = \"line_{line.sLineNumber}\" class = \"line-number\">{line.sLineNumber}</td><td style=\"padding-left:{totindent.ToString()}ch\">{lineBody.ToString()}</td></tr>" + Environment.NewLine);
                    else
                        htmlDoc.Append($"<tr><td class=\"fold-marker\"></td><td id = \"line_{line.sLineNumber}\" class = \"line-number\">{line.sLineNumber}</td><td style=\"padding-left:{totindent.ToString()}ch\">{lineBody.ToString()}</td></tr>" + Environment.NewLine);
                }
                else
                {
                    if (IsDef)
                        htmlDoc.Append($"<tr id={id} class=\"fold-header\" onclick=\"toggleFold('{id}')\"><td class=\"fold-marker\"><span id=\"arrow_{id}\" class=\"arrow-open\">▼</span></td><td id = \"line_{line.sLineNumber}\" class = \"line-number\">{line.sLineNumber}</td><td style=\"padding-left:{totindent.ToString()}ch\">{line.PlainLine}</td></tr>" + Environment.NewLine);
                    else if (IsInDef)
                        htmlDoc.Append($"<tr class=\"fold-body {id}\"><td class=\"fold-marker\"></td><td id = \"line_{line.sLineNumber}\" class = \"line-number\">{line.sLineNumber}</td><td style=\"padding-left:{totindent.ToString()}ch\">{line.PlainLine}</td></tr>" + Environment.NewLine);
                    else
                        htmlDoc.Append($"<tr><td class=\"fold-marker\"></td><td id = \"line_{line.sLineNumber}\" class = \"line-number\">{line.sLineNumber}</td><td style=\"padding-left:{totindent.ToString()}ch\">{line.PlainLine}</td></tr>" + Environment.NewLine);
                }

                if (IsInDef && !line.IsInDef)
                    IsInDef = false;

                if (IsDef)
                {
                    IsDef = false;
                    IsInDef = true;
                }
            }
            htmlDoc.Append(Environment.NewLine + _htmlClose);

            statusLeft.Text = $"{progInfo.ProgName}: {progInfo.NumberOfLines} lines";
            statusRight.Text = $"{progInfo.BasicDialect}";
            if (combProcFnFinder.Items.Count > 0)
                combProcFnFinder.SelectedIndex = 0;
            webView2.NavigateToString(htmlDoc.ToString());
        }
        private string? GetFormattedLineNumber(int docLine)
        {
            int index = docLine - 1;
            if (index < 0 || index >= _displayLines.Count)
                return null;

            var dl = _displayLines[index];
            return string.IsNullOrEmpty(dl.sLineNumber)
                ? null
                : dl.sLineNumber;
        }
        private void LoadFile(string filename)
        {
            progInfo.Filename = filename;
            bool _textFile = loadBasicOrText(filename, engine, formatOptions, progInfo);

            if (engine.CurrentListing != null)
                _loaded = true;
            //else
            //  return;
            label1.Visible = false;
            webView2.Visible = true;
            webView2.Enabled = true;

            if (!_textFile)
                BasicToHtml(engine);
            else
                TextToHtml(engine);
        }
        private async void Reload(BasToolsEngine engine)
        {
            if (!_loaded)
                return;

            var scrollY = await webView2.ExecuteScriptAsync("document.scrollingElement.scrollTop");
            int.TryParse(scrollY, out int savedScroll);

            if (!_textFile)
                BasicToHtml(engine);
            else
                TextToHtml(engine);

            await webView2.ExecuteScriptAsync($"document.scrollingElement.scrollTop = {savedScroll};");
        }
        private async void Form1_Shown(object? sender, EventArgs e)
        {
            // Ensure the WebView2 environment exists

            var env = await CoreWebView2Environment.CreateAsync();

            // Initialise the control
            await webView2.EnsureCoreWebView2Async(env);

            // Configure settings
            webView2.CoreWebView2.Settings.IsStatusBarEnabled = false;
            // disable the control's drag 'n' drop events
            webView2.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            webView2.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            webView2.CoreWebView2.Settings.AreHostObjectsAllowed = true;
            // Most important:
            webView2.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            webView2.AllowExternalDrop = false;

            toolStripTextBoxSearch.Width = getSrchboxWidth();

            // Now it's safe to navigate or inject HTML
            something();
        }
        /**************** Search ****************/
        private async void combProcFnFinder_SelectedIndexChanged(object sender, EventArgs e)
        {
            string target = combProcFnFinder.Text;

            if (target.StartsWith("DEF"))
                target = target.Substring(3).Trim();
            else
                return;

            StringBuilder id = new();
            if (target.StartsWith("PROC"))
                id.Append("proc_");
            else if (target.StartsWith("FN"))
                id.Append("fn_");
            else
                return;

            for (int i = id.Length - 1; i < target.Length && target[i] != '('; i++)
            {
                id.Append(target[i]);
            }
            //MessageBox.Show(id.ToString());
            await webView2.ExecuteScriptAsync($"document.getElementById('{id.ToString()}').scrollIntoView()");
        }
        private async void QuickSearch(bool backwards)
        {
            string text = toolStripTextBoxSearch.Text.Replace("'", "\\'");
            if (!_loaded || text.Length == 0) return;

            if (!backwards)
            {
                await webView2.CoreWebView2.ExecuteScriptAsync(
                    $"window.find('{text}', false, false, true, false, false, false);");
            }
            else
            {
                await webView2.CoreWebView2.ExecuteScriptAsync(
                    $"window.find('{text}', false, {backwards.ToString().ToLower()}, true, false, false, false);");
            }
            toolStripTextBoxSearch.Focus();
        }
        private async void DoGotoLine(string text)
        {
            text = text.Trim();
            if (!int.TryParse(text, out int dummy))
                return;

            if (_loaded && text.Length > 0)
            {
                await webView2.ExecuteScriptAsync($"document.getElementById('line_{text}').scrollIntoView()");
                contextMenuStrip1.Hide();
            }
        }
        // ********* Zoom Control helper ********
        private void ApplyZoom()
        {
            double factor = zoomSlider.Value / 100.0;
            webView2.ZoomFactor = factor;
            zoomPercentLabel.Text = $"{zoomSlider.Value}%";
        }
        //
        // *************** Form Controls etc. *************
        //
        private void comboBoxTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loaded)
                Reload(engine);
        }
        private void toolStripButtonMenu_Click(object sender, EventArgs e)
        {
            contextMenuStrip1.Show(toolStripLeft,
                new Point(menuStripRight.Bounds.Left, menuStripRight.Bounds.Bottom));
        }
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            FileOpen();
        }
        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }
        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (files != null && files.Length > 0)
            {
                LoadFile(files[0]);
            }
        }
        private void dragFileToLoadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            label1.Visible = true;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new AboutBox1())
            {
                dlg.ShowDialog(this);
            }
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    if (_loaded) label1.Visible = false;
                    break;
                case Keys.F:
                    if (e.Control) toolStripTextBoxSearch.Focus();
                    break;
                case Keys.F3:
                    bool backwards = e.Shift;
                    if (_loaded && toolStripTextBoxSearch.Text.Trim().Length > 0)
                    {
                        QuickSearch(backwards);
                    }
                    break;
                case Keys.Add:
                    zoomSlider.Value = Math.Min(zoomSlider.Maximum, zoomSlider.Value + 10);
                    ApplyZoom();
                    break;
                case Keys.Subtract:
                    zoomSlider.Value = Math.Max(zoomSlider.Minimum, zoomSlider.Value - 10);
                    ApplyZoom();
                    break;
            }
        }
        private async void toolStripTextBoxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.F3)
            {
                bool backwards = e.Shift;
                QuickSearch(backwards);
            }
        }
        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            QuickSearch(false);
        }
        private void toolStripButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (_loaded)
                Reload(engine);
        }
        private void toolStripButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (_loaded)
                Reload(engine);
        }
        private void advancedSearchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new frmAdvancedSearch())
            {
                dlg.ShowDialog(this);
            }
        }
        private void toolStripTextBoxGoto_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                DoGotoLine(toolStripTextBoxGoto.Text);
                return;
            }
            // Allow digits
            if (char.IsDigit(e.KeyChar))
                return;

            // Allow control keys that appear as KeyChar = 0
            if (char.IsControl(e.KeyChar))
                return;

            // Block everything else
            e.Handled = true;
        }
        private void gotoLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DoGotoLine(toolStripTextBoxGoto.Text);
        }
        private void toolStripLeft_SizeChanged(object sender, EventArgs e)
        {
            toolStripTextBoxSearch.Width = getSrchboxWidth();
        }
        private int getSrchboxWidth()
        {
            int toolbarWidth = toolStripLeft.Width;
            int totwidth = 0;
            foreach (ToolStripItem item in toolStripLeft.Items)
            {
                totwidth += item.Width;
            }
            totwidth -= toolStripTextBoxSearch.Width;

            if (toolbarWidth - totwidth > 100)
                return toolbarWidth - totwidth - 15;
            else
                return 100;
        }
    }
}
