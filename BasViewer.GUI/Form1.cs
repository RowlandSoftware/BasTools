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
        private string _htmlHeader;
        private string _htmlClose;
        private string _theme;
        public Form1(string[] args)
        {
            InitializeComponent();
            this.Shown += Form1_Shown;

            _args = args;
            bool flgZ80 = false;
            engine = new BasToolsEngine();
            progInfo = new ProgInfo(flgZ80, false, "");
            formatOptions = new FormattingOptions(true);
            _theme = "default";
            _htmlHeader = "<html>" + getCSS(_theme) + "<body><table>";
            _htmlClose = "</table></body></html>";

            Text = "BBC BASIC Viewer";

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
                        StringBuilder sb = new StringBuilder(_htmlHeader);
                        foreach (var line in engine.CurrentListing.Lines)
                        {
                            sb.Append("<tr><td>" + line.FormattedLineNumber + "</td><td>" + line.PlainDetokenisedLine + "</td></tr>");
                        }
                        sb.Append(Environment.NewLine + _htmlClose);
                        MessageBox.Show(sb.ToString());

                        toolStripStatusLabel1.Text = $"{engine.CurrentProgInfo.Filename}: {engine.CurrentProgInfo.NumberOfLines} lines";
                        webView2.NavigateToString(sb.ToString());
                    }
                }
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

                    //LoadDisplayLines(displayList);
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
            return string.IsNullOrEmpty(dl.FormattedLineNumber)
                ? null
                : dl.FormattedLineNumber;
        }
        private void LoadTextFile(string filename)
        {
            string html = _htmlHeader +
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
            MessageBox.Show(html);
            webView2.NavigateToString(html);
        }
        private string getCSS(string theme)
        {
            string css = "<style> body { font-family: Consolas;font-size:14; background: #111; color: #eee; } table { border-collapse: collapse; } td:first-child { color:LightGray; background-color:SlateGrey; padding-right: 4px; text-align:right; vertical-align: top;} td:last-child { white-space: pre-wrap; word-break: break-word; } </style>\r\n";
            return css;
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
    }
}
