using BasTools.Core;
using Microsoft.Web.WebView2.Core;
using System.Runtime.Intrinsics.Arm;
using System.Text;

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
        private bool loaded;
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
            loaded = false;
            _htmlClose = "</table></body></html>";
            _script = Environment.NewLine + "<script> function toggleFold(header) {const body = header.nextElementSibling; const arrow = header.querySelector(\".fold-arrow\"); if (body.style.display === \"none\") {body.style.display = \"block\"; arrow.textContent = \"?\";} else {body.style.display = \"none\"; arrow.textContent = \"?\";}} </script>" + Environment.NewLine;
            comboBoxTheme.SelectedIndex = 0;

            this.Text = "BBC BASIC Viewer";

            // DO NOT load files here
            // DO NOT touch WebView2 here
        }
        private void something()
        {
            if (_args != null && _args.Length > 0)
            {
                string? filename = _args.FirstOrDefault();
                if (!string.IsNullOrEmpty(filename))
                {
                    progInfo.Filename = filename;
                    loadBasicOrText(filename, engine, formatOptions, progInfo);

                    if (engine.CurrentListing != null)
                    {
                        loaded = true;
                        ListerOptions listerOptions = new ListerOptions(true, false, true, true); //bool indent, bool indentDefs, bool splitLines, bool pretty
                        List<DisplayLine> lines = engine.prepLinesForDisplay(listerOptions);

                        string htmlHeader = "<html><head>" + Themes.GetCss(comboBoxTheme.Text) + _script + "</head><body><table>";

                        StringBuilder sb = new StringBuilder(htmlHeader);
                        foreach (DisplayLine line in lines)
                        {
                            StringBuilder lineBody = new();
                            foreach (Token tok in BasToolsEngine.WalkTagged(line.LineBody))
                            {
                                if (tok.tag == null)
                                    lineBody.Append(tok.value);
                                else
                                {
                                    //lineBody.Append(tok.value);
                                    string tag = tok.tag.Substring(2, tok.tag.Length - 3);
                                    lineBody.Append($"<span class=\"{tag}\">");
                                    lineBody.Append(tok.value);
                                    lineBody.Append("</span>");
                                }
                                //if (line.Linenumber == 10) MessageBox.Show(lineBody.ToString());
                            }
                            bool IsDef = false;
                            bool IsInDef = false;
                            if (line.IsDef)
                            {
                                IsDef = true;
                                sb.Append("<div class=\"foldable\"><div class=\"fold-header\" onclick=\"toggleFold(this)\"><span class=\"fold-arrow\">?</span><span class=\"fold-title\">" + Environment.NewLine);
                                IsInDef = true;
                            }

                            int totindent = (line.Indent + line.DefIndent) * 2;
                            sb.Append("<tr><td>" + line.sLineNumber + "</td><td style=\"padding-left:" + totindent.ToString() + "ch\">" + lineBody.ToString() + "</td></tr>" + Environment.NewLine);

                            if (IsDef)
                            {
                                sb.Append("</span></div>" + Environment.NewLine + "<div class=\"fold-body\">");
                                IsDef = false;
                            }
                            if (IsInDef && !line.IsInDef)
                            {
                                sb.Append("</div>" + Environment.NewLine + "</div>" + Environment.NewLine);
                                IsInDef = false;
                            }
                        }
                        sb.Append(Environment.NewLine + _htmlClose);

                        toolStripStatusLabel1.Text = $"{progInfo.ProgName}: {progInfo.NumberOfLines} lines";
                        toolStripStatusLabel2.Text = $"{progInfo.BasicDialect}";
                        webView2.NavigateToString(sb.ToString());
                    }
                }
            }
            else
            {
                webView2.NavigateToString("<html><body style=\"background-color:blue\"><h3 style=\"color:white\">Drag 'n' drop files here</h3></body></html>");
            }
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
                }
            }
            catch
            {
                LoadTextFile(filename);
            }
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

            // Optional: configure settings
            webView2.CoreWebView2.Settings.AreDevToolsEnabled = true;
            webView2.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView2.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;

            // Now it's safe to navigate or inject HTML
            something();
        }
        private void comboBoxTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loaded)
                something();
        }
    }
}
