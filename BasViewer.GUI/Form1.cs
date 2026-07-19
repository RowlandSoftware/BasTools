using BasTools.Core;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualBasic.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.Arm;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static BasViewer.GUI.Form1;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.LinkLabel;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace BasViewer.GUI
{
    public record SearchOptions
    {
        public bool flgTextSearch { get; set; } = false;
        public bool whole_word { get; set; } = false;
        public bool match_case { get; set; } = false;
        public bool flgRealVars { get; set; } = false;
        public bool flgIntegers { get; set; } = false;
        public bool flgStrings { get; set; } = false;
        public bool flgProcs { get; set; } = false;
        public bool flgFns { get; set; } = false;
        public bool flgLiteralStrings { get; set; } = false;
        public bool flgRemContains { get; set; } = false;
        public bool flgStringContains { get; set; } = false;
    }
    public partial class Form1 : Form
    {
        private readonly string[] _args;
        ProgInfo? progInfo;
        FormattingOptions? formatOptions;
        BasToolsEngine? engine;
        private string _htmlClose;
        private string _script;
        private bool _loaded;
        private bool _textFile;
        private readonly frmAdvancedSearch advancedSearch;
        private SearchNavControl searchNav;
        private int currentMatchIndex;
        private List<SearchMatch> matches;
        private GoToLine gotoLineForm;

        public Form1(string[] args)
        {
            InitializeComponent();

            gotoLineForm = new GoToLine(this);   // pass Form1 to the popup

            menuStripRight.BackColor = Color.LightSkyBlue;
            toolStripLeft.Renderer = new NoBorderRenderer();
            menuStripRight.Renderer = new NoBorderRenderer();

            // Overrides
            toolStripTextBoxSearch.LostFocus += (s, e) =>
            {
                // Force WinForms to reset keyboard routing
                hiddenFocusCatcher.Focus();
            };

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

            // Init Search Navigator
            searchNav = new SearchNavControl();
            searchNav.Dock = DockStyle.Fill;
            panelSearchNav.Controls.Add(searchNav);
            searchNav.PrevClicked += () => NavigateMatch(-1);
            searchNav.NextClicked += () => NavigateMatch(+1);
            searchNav.StopClicked += () =>
            {
                panelSearchNav.Visible = false;
                ClearSearchHighlights();
            };

            // Z-order
            Controls.SetChildIndex(webView2, 0);
            Controls.SetChildIndex(topPanel, 1);
            Controls.SetChildIndex(panelSearchNav, 2);
            Controls.SetChildIndex(statusStrip1, 3);
            Controls.SetChildIndex(label1, 4);
            Controls.SetChildIndex(hiddenFocusCatcher, 5);

            // Force a full layout recalculation
            /*this.PerformLayout();
            this.ResumeLayout(true);*/

            _args = args;
            bool flgZ80 = false;
            engine = new BasToolsEngine();
            progInfo = new ProgInfo(flgZ80, false, "");
            formatOptions = new FormattingOptions(true);
            //_theme = "Dark";
            _loaded = false;
            _textFile = false;
            _htmlClose = Environment.NewLine + "</table></body></html>";
            _script = Themes.GetScript();
            comboBoxTheme.SelectedIndex = 0;

            // Advanced Search form
            advancedSearch = new frmAdvancedSearch();
            advancedSearch.Engine = engine;        // inject engine
            // set up callback
            advancedSearch.RunSearch = (term, opts) => DoSearch(term, opts);

            this.Text = "BBC BASIC Viewer";

            // DO NOT load files here
            // DO NOT touch WebView2 here
        }
        // Because WebView2 eats keypresses...
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool ctrlPressed = (keyData & Keys.Control) == Keys.Control;
            bool shiftPressed = (keyData & Keys.Shift) == Keys.Shift;

            ProcessKeypresses(keyData & Keys.KeyCode, ctrlPressed, shiftPressed);

            return base.ProcessCmdKey(ref msg, keyData);
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
                }
            }
            catch (BasToolsException ex)
            {
                MessageBox.Show($"{ex.Message}\n\n{ex.InnerException?.Message ?? ""}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            panelSearchNav.Visible = false;
            advancedSearch.Clear();

            return IsTextNotBasic;
        }
        private void TextToHtml(BasToolsEngine engine)
        {
            if (engine.CurrentListing == null) return;

            _loaded = true;
            combProcFnFinder.Items.Clear();

            bool pretty = toolStripBtnPrettyprint.Checked;

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
                        // convert tags to HTML spans
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
                        htmlDoc.Append($"<tr id={id} class=\"fold-header\" onclick=\"toggleFold('{id}')\"><td class=\"fold-marker\"><span id=\"arrow_{id}\" class=\"arrow-open\">▼</span></td><td id = \"line_{line.LineNumber}_0\" class = \"line-number\">{line.FormattedLineNumber}</td><td class=\"code\" style=\"padding-left:{totindent.ToString()}ch\">{lineBody.ToString()}</td></tr>" + Environment.NewLine);
                    else if (IsInDef)
                        htmlDoc.Append($"<tr class=\"fold-body {id}\"><td class=\"fold-marker\"></td><td id = \"line_{line.LineNumber}_0\" class = \"line-number\">{line.FormattedLineNumber}</td><td class=\"code\" style=\"padding-left:{totindent.ToString()}ch\">{lineBody.ToString()}</td></tr>" + Environment.NewLine);
                    else
                        htmlDoc.Append($"<tr><td class=\"fold-marker\"></td><td id = \"line_{line.LineNumber}_0\" class = \"line-number\">{line.FormattedLineNumber}</td><td class=\"code\" style=\"padding-left:{totindent.ToString()}ch\">{lineBody.ToString()}</td></tr>" + Environment.NewLine);
                }
                else
                {
                    if (IsDef)
                        htmlDoc.Append($"<tr id={id} class=\"fold-header\" onclick=\"toggleFold('{id}')\"><td class=\"fold-marker\"><span id=\"arrow_{id}\" class=\"arrow-open\">▼</span></td><td id = \"line_{line.LineNumber}_0\" class = \"line-number\">{line.FormattedLineNumber}</td><td class=\"code\" style=\"padding-left:{totindent.ToString()}ch\">{line.FormattedPlain}</td></tr>" + Environment.NewLine);
                    else if (IsInDef)
                        htmlDoc.Append($"<tr class=\"fold-body {id}\"><td class=\"fold-marker\"></td><td id = \"line_{line.LineNumber}_0\" class = \"line-number\">{line.FormattedLineNumber}</td><td class=\"code\" style=\"padding-left:{totindent.ToString()}ch\">{line.FormattedPlain}</td></tr>" + Environment.NewLine);
                    else
                        htmlDoc.Append($"<tr><td class=\"fold-marker\"></td><td id = \"line_{line.LineNumber}_0\" class = \"line-number\">{line.FormattedLineNumber}</td><td class=\"code\" style=\"padding-left:{totindent.ToString()}ch\">{line.FormattedPlain}</td></tr>" + Environment.NewLine);
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

            bool splitLines = toolStripBtnSplitlines.Checked;
            bool pretty = toolStripBtnPrettyprint.Checked;
            //MessageBox.Show($"Button status: {pretty} {splitLines}");

            ListerOptions listerOptions = new ListerOptions(true, false, splitLines, true); //bool indent, bool indentDefs, bool splitLines, bool pretty (ignored)
            List<DisplayLine> lines = engine.PrepLinesForDisplay(listerOptions);

            string htmlHeader = "<html><head>" + Themes.GetCss(comboBoxTheme.Text) + _script + "</head><body><table>";

            StringBuilder htmlDoc = new StringBuilder();

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
                        htmlDoc.Append($"<tr id={id} class=\"fold-header\" onclick=\"toggleFold('{id}')\"><td class=\"fold-marker\"><span id=\"arrow_{id}\" class=\"arrow-open\">▼</span></td><td id = \"line_{line.Id}\" class = \"line-number\">{line.sLineNumber}</td><td class=\"code\" style=\"padding-left:{totindent.ToString()}ch\">{lineBody.ToString()}</td></tr>" + Environment.NewLine);
                    else if (IsInDef)
                        htmlDoc.Append($"<tr class=\"fold-body {id}\"><td class=\"fold-marker\"></td><td id = \"line_{line.Id}\" class = \"line-number\">{line.sLineNumber}</td><td class=\"code\" style=\"padding-left:{totindent.ToString()}ch\">{lineBody.ToString()}</td></tr>" + Environment.NewLine);
                    else
                        htmlDoc.Append($"<tr><td class=\"fold-marker\"></td><td id = \"line_{line.Id}\" class = \"line-number\">{line.sLineNumber}</td><td class=\"code\" style=\"padding-left:{totindent.ToString()}ch\">{lineBody.ToString()}</td></tr>" + Environment.NewLine);
                }
                else
                {
                    if (IsDef)
                        htmlDoc.Append($"<tr id={id} class=\"fold-header\" onclick=\"toggleFold('{id}')\"><td class=\"fold-marker\"><span id=\"arrow_{id}\" class=\"arrow-open\">▼</span></td><td id = \"line_{line.Id}\" class = \"line-number\">{line.sLineNumber}</td><td class=\"code\" style=\"padding-left:{totindent.ToString()}ch\">{line.PlainLine}</td></tr>" + Environment.NewLine);
                    else if (IsInDef)
                        htmlDoc.Append($"<tr class=\"fold-body {id}\"><td class=\"fold-marker\"></td><td id = \"line_{line.Id}\" class = \"line-number\">{line.sLineNumber}</td><td class=\"code\" style=\"padding-left:{totindent.ToString()}ch\">{line.PlainLine}</td></tr>" + Environment.NewLine);
                    else
                        htmlDoc.Append($"<tr><td class=\"fold-marker\"></td><td id = \"line_{line.Id}\" class = \"line-number\">{line.sLineNumber}</td><td class=\"code\" style=\"padding-left:{totindent.ToString()}ch\">{line.PlainLine}</td></tr>" + Environment.NewLine);
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

            webView2.NavigateToString(htmlHeader + htmlDoc.ToString());
        }
        private void LoadFile(string filename)
        {
            progInfo.Filename = filename;
            _textFile = loadBasicOrText(filename, engine, formatOptions, progInfo);

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

            if (panelSearchNav.Visible)
                panelSearchNav.Visible = false;

            var scrollY = await webView2.ExecuteScriptAsync("document.scrollingElement.scrollTop");
            bool v = int.TryParse(scrollY, out int savedScroll);
            if (!v) savedScroll = 0;

            if (!_textFile)
                BasicToHtml(engine);
            else
                TextToHtml(engine);

            await webView2.ExecuteScriptAsync($"document.scrollingElement.scrollTop = {savedScroll};");
        }
        private async void Form1_Shown(object? sender, EventArgs e)
        {
            // Hide the navigator panel (which has to be visible initially to get layout right)
            panelSearchNav.Visible = false;

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
        /**************** Search and Navigation ****************/
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
        private async void NavigateMatch(int delta)
        {
            if (matches.Count == 0) return;

            currentMatchIndex += delta;

            if (currentMatchIndex < 0)
                currentMatchIndex = matches.Count - 1;
            else if (currentMatchIndex >= matches.Count)
                currentMatchIndex = 0;

            searchNav.UpdateStatus(currentMatchIndex, matches.Count);

            await ScrollToMatch(currentMatchIndex);
        }
        private async Task ScrollToMatch(int index)
        {
            var m = matches[index];

            // 1. Scroll to BASIC line number
            await webView2.CoreWebView2.ExecuteScriptAsync(
                $"document.getElementById('line_{m.Line}').scrollIntoView({{behavior:'smooth', block:'center'}});");

            // 2. Tell JS which highlighted span is the "current" one
            await webView2.CoreWebView2.ExecuteScriptAsync(
                $"window.search.scrollTo({index});");
        }
        private void ClearSearchHighlights()
        {
            webView2.CoreWebView2.ExecuteScriptAsync("window.search.clear();");
        }
        public async void DoGotoLine(string text)
        {
            text = text.Trim();
            if (!int.TryParse(text, out int dummy))
                return;

            if (_loaded && text.Length > 0)
            {
                await webView2.ExecuteScriptAsync($"document.getElementById('line_{text}_0').scrollIntoView()");
                contextMenuStrip1.Hide();
            }
        }

        //******* Advanced Search *********
        public class SearchMatch
        {
            public int Line { get; set; }
            public string LineId { get; set; }
            public int TokenIndex { get; set; }
            public int Offset { get; set; } = 0; // for text search only
            public int Length { get; set; }
            public string? Text { get; set; }
            public SymbolKind Kind { get; set; }
        }
        public void DoSearch(string term, SearchOptions opts)
        {
            if (string.IsNullOrWhiteSpace(term))
                return;

            if (matches == null)
                matches = new List<SearchMatch>();
            else
                matches.Clear();

            foreach (var kvp in engine.Symbols)
            {
                var sym = kvp.Value;

                /*if (sym.Name.StartsWith("lookup"))
                    MessageBox.Show($"Checking {term} against symbol {sym.Name}\n- type {sym.Kind}");*/

                // Filter by kind
                if (!MatchesKind(sym, opts))
                    continue;

                // For 'contains' text search
                StringComparison matchCase;
                if (opts.match_case)
                    matchCase = StringComparison.Ordinal;
                else
                    matchCase = StringComparison.OrdinalIgnoreCase;

                if (opts.flgTextSearch)
                {
                    if (sym.Name.Contains(term, matchCase))
                        getTextMatches(sym, term, opts, matches, engine.DisplayLines);
                }
                else
                {
                    bool found = sym.Name.Equals(term, StringComparison.Ordinal)
                    || sym.Name.Equals("." + term, StringComparison.Ordinal);

                    if (found)
                    {
                        //MessageBox.Show("Matched!");
                        getMatches(sym, matches, engine.DisplayLines);
                    }
                }
            }
            var distinctLines = matches.Select(m => m.Line).Distinct().Count();
            Log($"{DateAndTime.Now} Search '{term}': matches.Count = {matches.Count}, distinct lines = {distinctLines}");

            ApplySearchResults(term);
        }
        public static void getMatches(SymbolInfo sym, List<SearchMatch> matches, IReadOnlyList<DisplayLine> displayLines)
        {
            string name = sym.Name;
            int nameLength = name.Length;
            if (name.EndsWith("()"))
            {
                nameLength--; // adjust for arrays
                name = name.Substring(0, --nameLength);
            }

            var seen = new HashSet<(string lineId, int tokenIndex)>();

            foreach (var use in sym.Uses)
            {
                int targetLine = use.LineNumber;
                Log($"{name} - Searching for line {targetLine}");

                // Find the DisplayLine with this BASIC line number
                // (stop early because BASIC lines are sorted)
                DisplayLine? dl = null;
                foreach (var line in displayLines)
                {
                    //Log($"Trying {line.Linenumber}");
                    if (line.LineNumber == targetLine)
                    {
                        dl = line;
                        Log($"Matched line {targetLine}");
                        //break;
                    }
                    if (line.LineNumber > targetLine)
                        break;

                    if (dl == null)
                        continue; // might happen if program is corrupt

                    string raw = dl.LineBody;
                    if (string.IsNullOrEmpty(raw))
                        continue;

                    // Find the symbol name inside the raw line
                    int idx = -1;
                    foreach (Token tok in BasToolsEngine.WalkTagged(raw))
                    {
                        if (tok.tag != null)
                        {
                            idx++;
                            if (tok.value == name)
                            {
                                // skip if previously found
                                if (seen.Contains((line.Id, idx)))
                                    continue;

                                //Log($"Adding {name} at Indx {idx} in {line.Id}");
                                matches.Add(new SearchMatch
                                {
                                    Line = targetLine,
                                    LineId = line.Id,
                                    TokenIndex = idx,
                                    Offset = 0,
                                    Length = nameLength,
                                    Text = name,
                                    Kind = sym.Kind
                                });
                                seen.Add((line.Id, idx));
                                break;
                            }
                        }
                    }
                }
            }
        }
        public static void getTextMatches(SymbolInfo sym, string term, SearchOptions opts, List<SearchMatch> matches, IReadOnlyList<DisplayLine> displayLines)
        {
            // We know the term is contained (with case match), but have not checked if whole words

            var seen = new HashSet<(string lineId, int tokenIndex, int offset)>();

            foreach (SymbolUse use in sym.Uses)
            {
                Log($"Text search for {term} - found {sym.Name}");
                // Find the matching DisplayLine - so can figure out the index of the LiteralString or RemText token on that line (or lines if splitlines)
                int targetLine = use.LineNumber;
                DisplayLine? dl = null;
                foreach (var line in displayLines)
                {
                    //Log($"Trying {line.Linenumber}");
                    if (line.LineNumber == targetLine)
                    {
                        dl = line;
                        //Log($"Matched line {targetLine}");
                        //break;
                    }
                    if (line.LineNumber > targetLine)
                        break;

                    if (dl == null)
                        continue; // might happen if program is corrupt

                    string raw = dl.LineBody;
                    if (string.IsNullOrEmpty(raw))
                        continue;

                    // Find the symbol name inside the raw line
                    int idx = -1;
                    foreach (Token tok in BasToolsEngine.WalkTagged(raw))
                    {
                        if (tok.tag != null)
                        {
                            idx++;
                            //if (tok.value == "\"")
                            Log($"! Walking tags - ({sym.Kind}) {tok.tag} = {tok.value}");
                            if (sym.Kind == BasToolsEngine.InferKind(tok.tag, tok.value))
                            {
                                //foreach (int offset in FindAllMatches(sym.Name, term, opts.match_case))
                                    foreach (int offset in FindAllMatches(tok.value, term, opts.whole_word, opts.match_case))
                                    {
                                        // skip if previously found TODO
                                        if (seen.Contains((line.Id, idx, offset)))
                                        continue;

                                    Log($"Adding {term} at Indx {idx}, offset {offset}, length {term.Length} in {line.Id}");
                                    matches.Add(new SearchMatch
                                    {
                                        Line = targetLine,
                                        LineId = line.Id,
                                        TokenIndex = idx,
                                        Offset = offset,
                                        Length = term.Length,
                                        Text = term,
                                        Kind = sym.Kind
                                    });
                                    seen.Add((line.Id, idx, offset));
                                    //break;
                                }
                            }
                        }
                    }
                }
                
            }
        }
        public static IEnumerable<int> FindAllMatches(string text, string term, bool wholeWord, bool matchCase)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(term))
                yield break;

            // Escape the term so special regex chars are treated literally
            string escaped = Regex.Escape(term);

            // Whole-word or substring pattern
            string pattern = wholeWord ? $@"\b{escaped}\b" : escaped;
            RegexOptions options = RegexOptions.Compiled;
            if (!matchCase) options = options | RegexOptions.IgnoreCase;

            var regex = new Regex(pattern, options);

            foreach (Match m in regex.Matches(text))
            {
                yield return m.Index;   // same as IndexOf
            }
        }
        private async void ApplySearchResults(string term)
        {
            currentMatchIndex = 0;

            // Clear old highlights
            await webView2.CoreWebView2.ExecuteScriptAsync("window.search.clear();");

            if (matches.Count == 0)
            {
                panelSearchNav.Visible = false;
                ClearSearchHighlights();
                //advancedSearch.SetVariableEnabled(_textFile);
                advancedSearch.Show();
                advancedSearch.SetMessage("No matches. Please try again");
                advancedSearch.BringToFront();
                return;
            }

            var json = JsonSerializer.Serialize(matches);
            await webView2.CoreWebView2.ExecuteScriptAsync(
                $"window.search.applyMatches({json}, {currentMatchIndex});");

            searchNav.UpdateStatus(currentMatchIndex, matches.Count);

            // Show navigator            
            panelSearchNav.Visible = true;
        }
        private static bool MatchesKind(SymbolInfo sym, SearchOptions opts)
        {
            switch (sym.Kind)
            {
                case SymbolKind.Label:
                    return (sym.Name.EndsWith('%') && opts.flgIntegers)
                        || (!sym.Name.EndsWith('%') && opts.flgRealVars);

                case SymbolKind.StaticInt:
                case SymbolKind.IntVar:
                    return opts.flgIntegers;

                case SymbolKind.RealVar:
                    return opts.flgRealVars;

                case SymbolKind.StringVar:
                    return opts.flgStrings;

                case SymbolKind.LiteralString:
                    return opts.flgLiteralStrings || opts.flgStringContains;

                case SymbolKind.Proc:
                    return opts.flgProcs;

                case SymbolKind.Fn:
                    return opts.flgFns;

                case SymbolKind.RemText:
                    return opts.flgRemContains;

                default:
                    return false;
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
            label1.BringToFront();
            label1.Visible = true;
            label1.Focus();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new AboutBox1())
            {
                dlg.ShowDialog(this);
            }
        }
        // Probably never gets called... See protected override bool ProcessCmdKey
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            ProcessKeypresses(e.KeyCode, e.Control, e.Shift);
        }
        private void ProcessKeypresses(Keys keypress, bool ctrlPressed, bool shiftPressed)
        {
            switch (keypress)
            {
                case Keys.Escape:
                    if (_loaded) label1.Visible = false;
                    break;
                case Keys.F:
                    if (ctrlPressed)
                    {
                        toolStripTextBoxSearch.Focus();
                        toolStripTextBoxSearch.SelectAll();
                    }
                    break;
                case Keys.G:
                    if (ctrlPressed)
                    {
                        gotoLineForm.Show();
                        gotoLineForm.FocusTextbox();
                    }
                    break;
                case Keys.L:
                    if (ctrlPressed)
                    {
                        label1.BringToFront();
                        label1.Visible = true;
                        label1.Focus();
                    }
                    break;
                case Keys.F2:
                    ShowAdvancedSearch();
                    break;
                case Keys.F3:
                    if (_loaded && toolStripTextBoxSearch.Text.Trim().Length > 0)
                    {
                        QuickSearch(shiftPressed);
                    }
                    break;
                case Keys.Add:
                case Keys.Oemplus:
                    if (!toolStripTextBoxSearch.Focused)
                    {
                        zoomSlider.Value = Math.Min(zoomSlider.Maximum, zoomSlider.Value + 10);
                        ApplyZoom();
                    }
                    break;
                case Keys.Subtract:
                case Keys.OemMinus:
                    if (!toolStripTextBoxSearch.Focused)
                    {
                        zoomSlider.Value = Math.Max(zoomSlider.Minimum, zoomSlider.Value - 10);
                        ApplyZoom();
                    }
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
        private void toolStripBtnPrettyprint_CheckedChanged(object sender, EventArgs e)
        {
            if (_loaded)
                Reload(engine);
        }
        private void toolStripBtnSplitlines_CheckedChanged(object sender, EventArgs e) // split lines setting changed
        {
            if (_loaded)
                Reload(engine);
        }
        private void advancedSearchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowAdvancedSearch();
        }
        private void ShowAdvancedSearch()
        {
            if (_textFile)
            {
                MessageBox.Show("Advanced Search is not currently compatible\nwith text files in this version.", "Sorry", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            //advancedSearch.SetVariableEnabled(_textFile);
            advancedSearch.Show();
            advancedSearch.BringToFront();
            advancedSearch.SetTextFocus();
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
                return 100; // TODO Goes wrong if window too narrow
        }
        private static void Log(string message)
        {
            File.AppendAllText("search-debug.log", message + Environment.NewLine);
        }
        private void toolStripBtnPrevMatch_MouseDown(object sender, MouseEventArgs e)
        {
            //bool backwards = e.Button == MouseButtons.Right;
            QuickSearch(true);
        }
        private void toolStripBtnNextMatch_MouseDown(object sender, MouseEventArgs e)
        {
            QuickSearch(false);
        }
    }
}
