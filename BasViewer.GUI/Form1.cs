using BasTools.Core;
using Microsoft.VisualBasic;
using Microsoft.Web.WebView2.Core;
using System.Runtime.Intrinsics.Arm;
using System.Security.Policy;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace BasViewer.GUI
{
    public partial class Form1 : Form
    {
        private string[] _args;
        ProgInfo? progInfo;
        FormattingOptions? formatOptions;
        BasToolsEngine? engine;
        private List<DisplayLine> _displayLines = new();
        private string _htmlClose;
        private string _script;
        private bool _loaded;
        public Form1(string[] args)
        {
            InitializeComponent();
            this.Shown += Form1_Shown;

            _args = args;
            bool flgZ80 = false;
            engine = new BasToolsEngine();
            progInfo = new ProgInfo(flgZ80, false, "");
            formatOptions = new FormattingOptions(true);
            //_theme = "Dark";
            _loaded = false;
            _htmlClose = "</table></body></html>";
            _script = Environment.NewLine + "<script> function toggleFold(name) { const rows = document.querySelectorAll('.' + name); const arrow = document.getElementById('arrow_' + name); const isClosed = (arrow.textContent === \"▶\"); rows.forEach(r => { r.style.display = isClosed ? \"\" : \"none\"; }); arrow.textContent = isClosed ? \"▼\" : \"▶\"} </script>";
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
                    progInfo.Filename = filename;
                    LoadFile(filename);
                }
            }
            else
            {
                webView2.NavigateToString("<html><body style=\"background-color:blue\"><h3 style=\"color:white\">Drag 'n' drop files here</h3></body></html>");
            }
        }
        private void FileOpen()
        {
            string? filename = getFileOpen();
            if (filename == null)
                return;

            //if (progInfo == null) progInfo = new ProgInfo();

            progInfo.Filename = filename;
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

        internal void loadBasicOrText(string filename, BasToolsEngine engine, FormattingOptions formatOptions, ProgInfo progInfo)
        {
            try
            {
                Listing listing = engine.loadAndFormatFile(filename, formatOptions, progInfo);
                if (listing != null)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                {
                    //MessageBox.Show(ex.Message);
                    LoadTextFile(filename);
                }
            }
        }
        private void BasicToHtml(BasToolsEngine engine)
        {
            if (engine.CurrentListing == null) return;

            _loaded = true;
            combProcFnFinder.Items.Clear();

            ListerOptions listerOptions = new ListerOptions(true, false, true, true); //bool indent, bool indentDefs, bool splitLines, bool pretty
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
                if (IsDef)
                    htmlDoc.Append($"<tr id={id} class=\"fold-header\" onclick=\"toggleFold('{id}')\"><td class=\"fold-marker\"><span id=\"arrow_{id}\" class=\"arrow-open\">▼</span></td><td class = \"line-number\">{line.sLineNumber}</td><td style=\"padding-left:{totindent.ToString()}ch\">{lineBody.ToString()}</td></tr>" + Environment.NewLine);
                else if (IsInDef)
                    htmlDoc.Append($"<tr class=\"fold-body {id}\"><td class=\"fold-marker\"></td><td class = \"line-number\">{line.sLineNumber}</td><td style=\"padding-left:{totindent.ToString()}ch\">{lineBody.ToString()}</td></tr>" + Environment.NewLine);
                else
                    htmlDoc.Append($"<tr><td class=\"fold-marker\"></td><td class = \"line-number\">{line.sLineNumber}</td><td style=\"padding-left:{totindent.ToString()}ch\">{lineBody.ToString()}</td></tr>" + Environment.NewLine);

                if (IsInDef && !line.IsInDef)
                    IsInDef = false;

                if (IsDef)
                {
                    IsDef = false;
                    IsInDef = true;
                }
            }
            htmlDoc.Append(Environment.NewLine + _htmlClose);

            toolStripStatusLabel1.Text = $"{progInfo.ProgName}: {progInfo.NumberOfLines} lines";
            toolStripStatusLabel2.Text = $"{progInfo.BasicDialect}";
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
        private void LoadTextFile(string filename)
        {
            string htmlHeader = "<html>" + Themes.GetCss(comboBoxTheme.Text) + "<body><table>";
            string html = htmlHeader +
                "<tr><td>200</td><td>REM A sample program</td></tr>" +
                "<tr><td>100</td><td>REM A sample program</td></tr>" +
                "<tr><td>200</td><td>REM A sample program</td></tr>" +
                "<tr><td>300</td><td>REM A sample program</td></tr>" +
                "<tr><td>400</td><td>REM A sample program</td></tr>" +
                "<tr><td>500</td><td>REM A sample program</td></tr>" +
                "<tr><td>600</td><td>REM A sample program</td></tr>" +
                "<tr><td>700</td><td>REM A sample program</td></tr>" +
                "<tr><td>800</td><td>REM A sample program</td></tr>" +
                "<tr><td>900</td><td>REM A sample program</td></tr>" +
                "<tr><td>1000</td><td>REM A sample program</td></tr>" +
                _htmlClose;
            webView2.NavigateToString(html);
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

            // Now it's safe to navigate or inject HTML
            something();
        }
        private void comboBoxTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loaded)
                BasicToHtml(engine);
        }
        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            contextMenuStrip1.Show(toolStrip1,
                new Point(toolStripButton3.Bounds.Left, toolStripButton3.Bounds.Bottom));
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
                progInfo.Filename = files[0];
                LoadFile(files[0]);
            }
        }
        private void LoadFile(string filename)
        {
            loadBasicOrText(filename, engine, formatOptions, progInfo);

            if (engine.CurrentListing != null)
                _loaded = true;
            else
                return;
            label1.Visible = false;
            webView2.Visible = true;
            webView2.Enabled = true;

            BasicToHtml(engine);
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
        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (_loaded) label1.Visible = false;
            }
        }
        private async void combProcFnFinder_SelectedIndexChanged(object sender, EventArgs e)
        {
            string target = combProcFnFinder.Text;
            target = target.Substring(target.IndexOf(' ')).Trim(); // strip off DEF
            StringBuilder id = new();
            if (target.StartsWith("PROC"))
                id.Append("proc_");
            else
                id.Append("fn_");

            for (int i = id.Length - 1; i < target.Length && target[i] != '('; i++)
            {
                id.Append(target[i]);
            }
            //MessageBox.Show(id.ToString());
            await webView2.ExecuteScriptAsync($"document.getElementById('{id.ToString()}').scrollIntoView()");
        }
        private async void toolStripTextBoxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                bool backwards = e.Shift;
                BasicSearch(backwards);
            }
        }
        private async void BasicSearch(bool backwards)
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
        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            BasicSearch(false);
        }
    }
}
