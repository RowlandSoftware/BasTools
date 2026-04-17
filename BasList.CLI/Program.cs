using System.Diagnostics;

namespace BasList.CLI
{
    using BasTools.Core;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using static System.Runtime.InteropServices.JavaScript.JSType;
    using static System.Windows.Forms.AxHost;
    using static System.Windows.Forms.LinkLabel;
    using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

    //***************** CommandSwitches *****************
    public class CommandSwitches
    {
        // switches for detokenisation
        private bool _basicV;
        private bool _notBasicV;
        // switches for formatting
        public bool FlgAddNums;
        public bool FlgIndent;
        public bool FlgEmphDefs;
        public bool Align;
        // switches for listing
        public bool NoFormat;
        public bool NoLineNumbers;
        public bool Bare;
        public bool SplitLines;
        public bool AssemblerColumns;
        private int _columnWidth;
        public bool Pretty;
        private bool _flgPause;
        // switches for filtering listings
        public int FromLine;
        public int ToLine;
        public bool FlgIf;
        public bool FlgIfX;
        public bool FlgList;
        public List<string> DirectiveParams;
        // switches for appearance
        public bool Clear;
        public bool FlgDark;
        public ConsoleColor ForeColor;
        public ConsoleColor BackColor;
        // debug
        public bool Debug;
        public CommandSwitches()
        {
            _basicV = false;
            _notBasicV = false;
            FlgAddNums = false;
            FromLine = 0;
            ToLine = -1;
            FlgIndent = false;
            FlgEmphDefs = false;
            Align = false;
            AssemblerColumns = false;
            _columnWidth = 0;
            NoFormat = false;
            NoLineNumbers = false;
            Bare = false;
            SplitLines = false;
            Pretty = false;
            Clear = false;
            FlgIf = false;
            FlgIfX = false;
            FlgList = false;
            FlgPause = false;
            FlgDark = true;
            DirectiveParams = new();
            Debug = false;
        }
        public bool BasicV
        {
            get => _notBasicV ? false : _basicV;
            set => _basicV = value;
        }
        public bool NotBasicV
        {
            set => _notBasicV = value;
        }
        public void SetColumnWidth(int width)
        {
            _columnWidth = width;
        }
        public int ExtraColumnWidth
        {
            get => _columnWidth;
        }
        public bool FlgPause
        {
            get => !Console.IsOutputRedirected && _flgPause;
            set => _flgPause = value;
        }
        public void checkFromTo()
        {
            if (FromLine > ToLine) (FromLine, ToLine) = (ToLine, FromLine); // swop using tuple
        }
        public void SwopIfLight()
        {
            if (!FlgDark) (ForeColor, BackColor) = (BackColor, ForeColor);
        }
        public FormattingOptions copyToFormatOptions()
        {
            FormattingOptions opts = new FormattingOptions();
            opts.NoFormat = NoFormat;
            opts.Align = Align;
            opts.FlgAddNums = FlgAddNums;
            opts.SplitLines = SplitLines;
            opts.Bare = Bare;
            opts.FlgEmphDefs = FlgEmphDefs;
            opts.FlgIndent = FlgIndent;
            opts.AssemblerColumns = AssemblerColumns;
            opts.ExtraColumnWidth = ExtraColumnWidth;

            return opts;
        }
    }
    // This maps semantic tags to actual console colours
    static class ConsoleColorMap
    {
        private static readonly Dictionary<string, ConsoleColor> _map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [SemanticTags.Keyword] = ConsoleColor.Blue,
            [SemanticTags.IndentingKeyword] = ConsoleColor.Blue,
            [SemanticTags.OutdentingKeyword] = ConsoleColor.Blue,
            [SemanticTags.InOutKeyword] = ConsoleColor.Blue,
            [SemanticTags.BuiltInFn] = ConsoleColor.Blue,
            [SemanticTags.StringLiteral] = ConsoleColor.Green,
            [SemanticTags.Number] = ConsoleColor.White,
            [SemanticTags.HexNumber] = ConsoleColor.White,
            [SemanticTags.BinaryNumber] = ConsoleColor.White,
            [SemanticTags.Variable] = ConsoleColor.Magenta,
            [SemanticTags.StaticInteger] = ConsoleColor.DarkYellow,
            [SemanticTags.RemText] = ConsoleColor.Yellow,
            [SemanticTags.AssemblerComment] = ConsoleColor.Yellow,
            [SemanticTags.EmbeddedData] = ConsoleColor.White,
            [SemanticTags.ProcName] = ConsoleColor.Cyan,
            [SemanticTags.FunctionName] = ConsoleColor.Cyan,
            [SemanticTags.Label] = ConsoleColor.Magenta,
            [SemanticTags.Register] = ConsoleColor.Green,
            [SemanticTags.Mnemonic] = ConsoleColor.Blue,
            [SemanticTags.Operator] = ConsoleColor.Red,
            [SemanticTags.IndirectionOperator] = ConsoleColor.White,
            [SemanticTags.ImmediateOperator] = ConsoleColor.White,
            [SemanticTags.LineNumber] = ConsoleColor.DarkGray,
            [SemanticTags.StarCommand] = ConsoleColor.White,
            [SemanticTags.StatementSep] = ConsoleColor.White,
            [SemanticTags.ListSep] = ConsoleColor.White,
            [SemanticTags.OpenBracket] = ConsoleColor.White,
            [SemanticTags.CloseBracket] = ConsoleColor.White,
        };
        public static bool TryGetColor(string tag, out ConsoleColor color, bool darkMode)
        {
            //Console.WriteLine($" -- {tag} --");
            if (_map.TryGetValue(tag, out color))
            {
                if (!darkMode)
                {
                    switch (color)
                    {
                        case ConsoleColor.Yellow: color = ConsoleColor.DarkYellow; break;
                        case ConsoleColor.White: color = ConsoleColor.Black; break;
                    }
                }
                return true;
            }
            return false;
        }
    }
    class ListerState
    {
        public int LineCount;
        public bool Printme;
        public bool Listme;
        public int Indent;
        public int PendingIndent;
        public bool MultiLineIf;
        public ConsoleColor CurrentForeground;
        public ConsoleColor CurrentBackground;
        public ListerState()
        {
            LineCount = 0;
            Printme = false;
            Listme = false;
            Indent = 0;
            PendingIndent = 0;
            MultiLineIf = false;
            CurrentForeground = ConsoleColor.White;
            CurrentBackground = ConsoleColor.Black;
        }
        public ListerState(ListerState other)
        {
            LineCount = other.LineCount;
            Printme = other.Printme;
            Indent = other.Indent;
            PendingIndent = other.PendingIndent;
            Listme = other.Listme;
            MultiLineIf = other.MultiLineIf;
            CurrentForeground = other.CurrentForeground;
            CurrentBackground = other.CurrentBackground;
        }
    }
    public class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                help();
                Environment.Exit(0);
            }

            CommandSwitches switches = new();
            string filename = string.Empty;
            string format = string.Empty;

            //******** readCommandSwitches ********

            readCommandSwitches(args, switches, ref filename, ref format);

            // Show message
            Console.Error.WriteLine("Processing, please wait...");

            BasToolsEngine engine = new BasToolsEngine();
            bool flgZ80 = false;
            ProgInfo progInfo = new(flgZ80, switches.BasicV, filename);
            FormattingOptions formatOptions = switches.copyToFormatOptions();

            try
            {
                Listing formattedListing = engine.loadAndFormatFile(filename, formatOptions, progInfo);
                //Console.WriteLine($"I got {formattedListing.Lines.Count} lines");

                displayProgramLines(formattedListing, switches, progInfo);
            }
            catch (BasToolsException ex)
            {
                Console.Error.WriteLine($"{ex.Message}\n\n{ex.InnerException?.Message ?? ""}");
            }
        }

        //**************** Get User Input *****************
        static void readCommandSwitches(string[] args, CommandSwitches switches, ref string filename, ref string format)
        {
            foreach (string arg in args)
            {
                bool recognised = false;
                if ((arg.StartsWith('/') || arg.StartsWith('-')) && arg.Length > 1)
                {
                    string arg2 = arg.Substring(1).ToUpper(); // remove the / or -
                    string arg1 = string.Empty;
                    string arg3 = string.Empty;
                    int x = arg2.IndexOf('=');                // split at '=' or ':' if present
                    x = x >= 0 ? x : arg2.IndexOf(':');
                    if (x >= 0)
                    {
                        arg1 = arg2.Substring(0, x);
                        arg3 = arg2.Substring(x + 1);
                        if ("FILE".StartsWith(arg1)) { filename = arg3; recognised = true; }
                        if ("INDENT".StartsWith(arg1))
                        {
                            recognised = true;
                            if ("ALL".StartsWith(arg3))
                            {
                                switches.FlgIndent = true;
                                switches.FlgEmphDefs = true;
                            }
                            else if ("LOOPS".StartsWith(arg3))
                            {
                                switches.FlgIndent = true;
                                switches.FlgEmphDefs = false;
                            }
                            else if ("DEFS".StartsWith(arg3))
                            {
                                switches.FlgIndent = false;
                                switches.FlgEmphDefs = true;
                            }
                        }
                        if ("COLUMNS".StartsWith(arg1))
                        {
                            switches.AssemblerColumns = true;
                            recognised = true;

                            if (int.TryParse(arg3, out int width))
                            {
                                if (width >= -5 && width <= 20)
                                {
                                    switches.SetColumnWidth(width);
                                }
                                else
                                {
                                    Console.WriteLine($"Extra assembler column width {width} not between -5 and 20 inc.\n - Using default ({switches.ExtraColumnWidth})");
                                }
                            }
                        }
                    }
                    if (arg2 == "V") { switches.BasicV = true; recognised = true; }
                    if ("NOTBASICV".StartsWith(arg2)) { switches.NotBasicV = true; recognised = true; }
                    if (arg2 == "?" || "HELP".StartsWith(arg2)) { help(); Environment.Exit(0); }
                    if ("ADDNUMBERS".StartsWith(arg2)) { switches.FlgAddNums = true; recognised = true; }
                    if ("BARE".StartsWith(arg2)) { switches.Bare = true; recognised = true; }
                    if ("SPLITLINES".StartsWith(arg2)) { switches.SplitLines = true; recognised = true; }
                    if ("PAUSE".StartsWith(arg2)) { switches.FlgPause = true; recognised = true; }
                    if ("PRETTYPRINT".StartsWith(arg2)) { switches.Pretty = true; recognised = true; }
                    if ("ALIGN".StartsWith(arg2)) { switches.Align = true; recognised = true; }
                    if ("COLUMNS".StartsWith(arg2)) { switches.AssemblerColumns = true; recognised = true; }
                    if ("CLS".StartsWith(arg2)) { switches.Clear = true; recognised = true; }
                    if ("CLEAR".StartsWith(arg2)) { switches.Clear = true; recognised = true; }
                    if ("INDENT".StartsWith(arg2)) { switches.FlgIndent = true; switches.FlgEmphDefs = true; recognised = true; }
                    if ("NONUMBERS".StartsWith(arg2)) { switches.NoLineNumbers = true; recognised = true; }
                    if ("NOFORMAT".StartsWith(arg2)) { switches.NoFormat = true; recognised = true; }
                    if ("DARK".StartsWith(arg2)) { switches.FlgDark = true; recognised = true; }
                    if ("LIGHT".StartsWith(arg2)) { switches.FlgDark = false; recognised = true; }
                    if ("DEBUG".StartsWith(arg2)) { switches.Debug = true; recognised = true; }
                    if (!recognised && !switches.Bare) Console.Error.WriteLine("Option " + arg.ToLower() + " not recognised");
                }
                // not a switch ...
                if (IsNumeric(arg))
                {
                    try
                    {
                        recognised = true;
                        if (arg.EndsWith(','))
                        {
                            switches.FromLine = int.Parse(arg.Substring(0, arg.Length - 1));
                            switches.ToLine = 0xFFFF;
                        }
                        else if (arg.StartsWith(','))
                            switches.ToLine = int.Parse(arg.Substring(1));
                        else if (arg.Contains(','))
                        {
                            string[] temp = arg.Split(',');
                            switches.FromLine = int.Parse(temp[0]);
                            switches.ToLine = int.Parse(temp[1]);
                        }
                        else
                        {
                            if (switches.FromLine == 0) switches.FromLine = int.Parse(arg); else switches.ToLine = int.Parse(arg);
                        }

                        if (switches.ToLine == -1) switches.ToLine = switches.FromLine;
                        switches.checkFromTo(); // reverse From Line and To Line if wrong way round
                    }
                    catch (System.FormatException fe)
                    {
                        Console.Error.WriteLine("Line numbers not in correct format");
                        //Console.WriteLine(fe.Message);
                        Environment.Exit(0);
                    }
                }
                else if (filename.Length == 0 && !recognised) // This is where we pick up the filename if not already found
                {
                    if (arg != "IF" && arg != "IFX" && arg != "LIST")
                        filename = arg;
                }
                if (string.Equals(arg, "IF", StringComparison.OrdinalIgnoreCase))
                {
                    switches.FlgIf = true;
                    getDirectiveParams(args, switches);
                    break;
                }
                else if (string.Equals(arg, "IFX", StringComparison.OrdinalIgnoreCase))
                {
                    switches.FlgIfX = true;
                    getDirectiveParams(args, switches);
                    break;
                }
                else if (string.Equals(arg, "LIST", StringComparison.OrdinalIgnoreCase))
                {
                    switches.FlgList = true;
                    getDirectiveParams(args, switches);
                    break;
                }
            }
            if (switches.NoFormat) switches.SplitLines = false;

            if (switches.ToLine < 0) switches.ToLine = switches.BasicV ? 0xFFFF : 0x7FFF;
            if (switches.FlgList) { switches.FromLine = 0; switches.ToLine = 0xFFFF; } // line numbers ignored for LIST

            // no filename found:
            if (filename.Length == 0)
            {
                Console.Error.WriteLine("Error: No filename found");
                help();
                Environment.Exit(0);
            }
            if (switches.Bare) switches.FlgPause = false;
        }
        static void getDirectiveParams(string[] args, CommandSwitches s)
        {
            s.DirectiveParams = new();

            int i = 0;
            while (i < args.Length &&
            !args[i].Equals("IF", StringComparison.InvariantCultureIgnoreCase) &&
            !args[i].Equals("IFX", StringComparison.InvariantCultureIgnoreCase) &&
            !args[i].Equals("LIST", StringComparison.InvariantCultureIgnoreCase))
            { i++; }

            // Move past the directive keyword
            if (i < args.Length) i++;

            for (int j = i; j < args.Length; j++)
                s.DirectiveParams.Add(args[j]);
        }

        //****************** Display the Output ***********
        private static void displayProgramLines(Listing formattedListing, CommandSwitches switches, ProgInfo progInfo)
        {
            ListerState state = new(); // this sets initial conditions

            switches.BackColor = ConsoleColor.Black;
            switches.ForeColor = ConsoleColor.White;
            switches.SwopIfLight();

            Console.ForegroundColor = switches.ForeColor;
            Console.BackgroundColor = switches.BackColor;
            state.CurrentForeground = switches.ForeColor;
            state.CurrentBackground = switches.BackColor;

            if (switches.Clear && !Console.IsOutputRedirected) Console.Clear();
            int linesprinted = 0;
            //
            // ******** LISTING STARTS HERE ********
            //
            string format = progInfo.BasicDialect;
            if (!switches.Bare && !switches.FlgList) Console.WriteLine($"\nListing '{progInfo.Filename}' from line {switches.FromLine} to {switches.ToLine} ({format} format)\n");

            string sIndent = string.Empty;

            foreach (ProgramLine progline in formattedListing.Lines)
            {
                if (progline.LineNumber >= switches.FromLine && progline.LineNumber <= switches.ToLine)
                {
                    // set flags
                    state.Printme = false;
                    if (switches.FlgIf)
                    {
                        foreach (string param in switches.DirectiveParams)
                        {
                            if (progline.NoSpacesLine.Contains(param, StringComparison.OrdinalIgnoreCase)) { state.Printme = true; continue; }
                        }
                    }
                    if (switches.FlgIfX)
                    {
                        string cleanline = progline.PlainDetokenisedLine;
                        foreach (string param in switches.DirectiveParams)
                        {
                            if (cleanline.Contains(param, StringComparison.Ordinal)) { state.Printme = true; continue; }
                        }
                    }
                    if (switches.FlgList)
                    {
                        if (progline.IsDef)
                        {
                            state.Listme = nameMatch(progline.TaggedLine, switches); // automatically cancels ListMe at DEF if no match
                        }
                    }

                    bool insideIf = switches.FlgIf || switches.FlgIfX;
                    bool shouldPrint =
                        (!insideIf && !switches.FlgList) ||
                        (insideIf && state.Printme) ||
                        (switches.FlgList && state.Listme);

                    if (shouldPrint)
                    {
                        //******************* WRITE ONE LINE TO CONSOLE ************************

                        if (!switches.SplitLines)
                        {
                            printLineOut(progline, switches, state, ref linesprinted);
                        }
                        else // SplitLines
                        {
                            bool first = true;

                            Listing sections = new(new List<ProgramLine>());

                            // generate a 'min-program-listing' from the sections
                            foreach (string taggedSection in SplitStatements(progline.FormattedTagged))
                            {
                                ProgramLine line = new(progline);
                                line.TaggedLine = taggedSection;
                                sections.Lines.Add(line);
                            }

                            // Print normally if only one section - not necessary, just more efficient
                            if (sections.Lines.Count == 1)
                            {
                                printLineOut(progline, switches, state, ref linesprinted);
                            }
                            else
                            {
                                // Call the Formatter to format these 'lines'
                                BasToolsEngine engine = new BasToolsEngine();

                                engine.formatLines(sections, switches.copyToFormatOptions(), progline.fstate, progInfo, true);

                                foreach (ProgramLine line in sections.Lines)
                                {
                                    int printedLineLength = 0;
                                    PrintLineNumber(line, switches, ref printedLineLength, first);
                                    first = false;
                                    PrintIndents(line, ref printedLineLength, switches);

                                    PrintOut(line.TaggedLine.TrimStart(), state, switches, ref printedLineLength, ref linesprinted);

                                    if (switches.FlgPause)
                                    {
                                        switch (CheckForPause(switches, ref linesprinted))
                                        {
                                            case ConsoleKey.Spacebar: linesprinted = 0; break;
                                            case ConsoleKey.Enter: linesprinted--; break;
                                            case ConsoleKey.Escape: ResetAndExit(switches); break;
                                        }
                                    }
                                }
                            }
                        }
                        // After printing the line, turn off printing PROC (so don't suppress ENDPROC)
                        if (switches.FlgList && state.Listme)
                        {
                            if (!progline.IsDef && !progline.IsInDef)
                            {
                                state.Listme = false;
                            }
                        }
                        #region debug
                        if (switches.Debug)
                        {
                            Console.WriteLine($"{progline.LineNumber} -");

                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.Write($"  Plain:     ");
                            Console.ForegroundColor = state.CurrentForeground;
                            Console.WriteLine($"{progline.PlainDetokenisedLine}");

                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.Write($"  Formatted: ");
                            Console.ForegroundColor = state.CurrentForeground;
                            Console.WriteLine($"{progline.FormattedPlain}");

                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.Write($"  Tagged:    ");
                            Console.ForegroundColor = state.CurrentForeground;
                            Console.WriteLine($"{progline.TaggedLine}");

                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.Write($"  Formatted: ");
                            Console.ForegroundColor = state.CurrentForeground;
                            Console.WriteLine($"{progline.FormattedTagged}");

                            Console.WriteLine();
                        }
                        #endregion
                    } // end shouldprint
                }
            }
        }
        // ******** PrintOut - handles plain and PrettyPrint ********
        static void PrintOut(string line, ListerState state, CommandSwitches switches, ref int printedLineLength, ref int linesprinted)
        {
            // Line contents
            foreach (Token tok in BasToolsEngine.WalkTagged(line))
            {
                if (tok.tag != null && ConsoleColorMap.TryGetColor(tok.tag, out var c, switches.FlgDark))
                {
                    if (switches.Pretty) Console.ForegroundColor = c;

                    Console.Write(tok.value);

                    Console.BackgroundColor = switches.BackColor;
                    Console.ForegroundColor = switches.ForeColor;
                }
                else
                {
                    Console.Write(tok.value);
                }
            }
            Console.WriteLine("");
            int windowWidth = Console.WindowWidth;
            int rows = (printedLineLength + (windowWidth - 1)) / windowWidth;

            linesprinted += rows;
        }
        static void printLineOut(ProgramLine progline, CommandSwitches switches, ListerState state, ref int linesprinted)
        {
            int printedLineLength = 0;

            if (!switches.NoFormat)
            {
                // Normal behaviour
                PrintLineNumber(progline, switches, ref printedLineLength, true);
                PrintIndents(progline, ref printedLineLength, switches);
                PrintOut(progline.FormattedTagged, state, switches, ref printedLineLength, ref linesprinted);
            }
            else
            {
                // plain printout without additional spaces
                string printedLine = (switches.NoLineNumbers ? "" : (progline.FormattedLineNumber +
                    (!switches.NoFormat ? ' ' : ""))) +
                    (switches.FlgIndent ? new string(' ', progline.IndentLevel * 2) : "") +
                    (switches.FlgEmphDefs ? new string(' ', progline.DefIndent * 2) : "") +
                    (switches.NoFormat ? progline.PlainDetokenisedLine : progline.FormattedPlain);
                Console.WriteLine(printedLine);

                printedLineLength += printedLine.Length;

                int windowWidth = Console.WindowWidth;
                int rows = (printedLineLength + (windowWidth - 1)) / windowWidth;

                linesprinted += rows;
            }
            // Deal with pausing
            if (switches.FlgPause)
            {
                switch (CheckForPause(switches, ref linesprinted))
                {
                    case ConsoleKey.Spacebar: linesprinted = 0; break;
                    case ConsoleKey.Enter: linesprinted--; break;
                    case ConsoleKey.Escape: ResetAndExit(switches); break;
                }
            }
        }
        static void PrintLineNumber(ProgramLine progline, CommandSwitches switches, ref int printedLineLength, bool first)
        {
            // Line preamble
            if (!switches.NoLineNumbers && progline.FormattedLineNumber.Length > 0)
            {
                string? ln = progline.FormattedLineNumber;
                printedLineLength = ln.Length + (switches.NoFormat ? 0 : 1);

                if (!first) ln = new string(' ', ln.Length);
                ln = SemanticTags.LineNumber + ln + SemanticTags.Reset;

                foreach (Token tok in BasToolsEngine.WalkTagged(ln)) // retrieve line no. colour
                {
                    if (tok.tag != null && ConsoleColorMap.TryGetColor(tok.tag, out var c, switches.FlgDark))
                    {
                        if (switches.Pretty) { Console.ForegroundColor = c; }
                        Console.Write(tok.value);
                    }
                    else
                        Console.Write(ln);
                    Console.ForegroundColor = switches.ForeColor;

                    if (!switches.NoFormat)
                        Console.Write(' ');
                }
            }
        }
        static void PrintIndents(ProgramLine progline, ref int printedLineLength, CommandSwitches switches)
        {
            if (!progline.InAsm)
            {
                if (switches.FlgIndent)
                {
                    Console.Write(new string(' ', progline.IndentLevel * 2)); // ignore indents in assembler - assume is in [OPT opt% loop
                    printedLineLength += progline.IndentLevel * 2;
                }
            }
            if (switches.FlgEmphDefs)
            {
                Console.Write(new string(' ', progline.DefIndent * 2));
                printedLineLength += progline.DefIndent * 2;
            }
            
        }
        static bool nameMatch(string taggedline, CommandSwitches switches)
        {
            if (!switches.FlgList) return false;

            var (type, procName) = readProcFnName(taggedline);

            if (procName == null) return false;
            procName = (type == SemanticTags.ProcName ? "PROC" : "FN") + procName;

            for (int i = 0; i < switches.DirectiveParams.Count; i++)
            {
                //Console.WriteLine($"Matching {procName} - {switches.DirectiveParams[i]}");
                if (procName.Equals(switches.DirectiveParams[i], StringComparison.InvariantCultureIgnoreCase)) return true;
            }
            return false;
        }
        static (string type, string ProcFnName) readProcFnName(string taggedline)
        {
            foreach (Token tok in BasToolsEngine.WalkTagged(taggedline))
            {
                if (tok.tag is SemanticTags.ProcName or SemanticTags.FunctionName) return (tok.tag, tok.value);
            }
            return (null!, null!);
        }
        static List<string> SplitStatements(string code)
        {
            var result = new List<string>();
            var sb = new StringBuilder();

            var tokens = BasToolsEngine.WalkTagged(code).ToList();
            for (int i = 0; i < tokens.Count; i++)
            {
                Token tok = tokens[i];
                sb.Append(tok.tag + tok.value);
                if (!string.IsNullOrEmpty(tok.tag))
                    sb.Append("{/}");

                if (i < tokens.Count - 1 && tok.tag == SemanticTags.StatementSep && tokens[i + 1].value == "THEN")
                    continue;

                if (tok.tag != SemanticTags.StatementSep && i < tokens.Count - 1 && tokens[i + 1].value == "ELSE")
                {
                    result.Add(sb.ToString().TrimEnd());
                    sb.Clear();
                }

                if (tok.isLast || tok.tag == SemanticTags.StatementSep || tok.value == "ELSE" || tok.value == "THEN")
                {
                    result.Add(sb.ToString().TrimEnd());
                    sb.Clear();
                }
            }
            return result;
        }
        //**** Utility functions ******
        private static ConsoleKey CheckForPause(CommandSwitches switches, ref int linesprinted)
        {
            if (linesprinted == Console.WindowHeight - 4)
            {
                Console.ForegroundColor = switches.ForeColor;

                string prompt = " -- Enter - next line | Space - Continue | Esc - End --";
                if (Console.WindowWidth <= prompt.Length)
                {
                    prompt = "[Enter | Space | Esc]";
                }
                Console.Write(prompt);
                // Read until a valid key is pressed
                ConsoleKey key;
                while (true)
                {
                    var info = Console.ReadKey(intercept: true);
                    key = info.Key;

                    if (key == ConsoleKey.Spacebar ||
                        key == ConsoleKey.Enter ||
                        key == ConsoleKey.Escape)
                    {
                        break; // valid key
                    }
                }
                // Clear the prompt once
                ClearCurrentConsoleLine();
                return key;
            }
            return ConsoleKey.None;
        }
        private static void ClearCurrentConsoleLine()
        {
            if (Console.IsOutputRedirected) return;
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int i = 0; i < Console.WindowWidth; i++)
                Console.Write(" ");
            Console.SetCursorPosition(0, currentLineCursor);
        }
        static bool IsNumeric(string s) // special for decoding line numbers - includes comma
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (!"0123456789,".Contains(s.Substring(i, 1))) return false;
            }
            return true;
        }
        static void help()
        {
            string vs = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion ?? "1.1.0"; // ?? = null coalescing operator. //requires ref to System.Windows.Forms

            Console.WriteLine($"\nBasList vs {vs} (C) Andrew Rowland 2022-26");
            Console.WriteLine("\nLists a BBC BASIC program file\n");
            Console.WriteLine("BasList [/file=]filename ([[from line] [to line]) | [line,line]]) [Options] ([IF ...] | [IFX ...] | [LIST ...])");
            //Console.WriteLine("BasList [/file=]filename [/V] [/addnumbers] [/align] [/indent] [/nonumbers] [/noformat] [/bare] [/pause] [/prettyprint] [(cls | clear)]");
            //Console.WriteLine("BasList [/file=]filename [(/dark | /light)]");
            Console.WriteLine("BasList [/? | -h]  Display help\n");
            Console.WriteLine("  [/file=]filename");
            Console.WriteLine("                   Specifies filename of tokenised BASIC program.");
            Console.WriteLine("                   Filename to follow '=' without spaces. Quote if contains spaces.");
            Console.WriteLine("                   '/file=' may be omitted if filename is first item");

            Console.WriteLine("\nOPTIONS");
            Console.WriteLine("-------");
            Console.WriteLine("  /V               Interpret BASIC V assembler (May be auto-detected)");
            Console.WriteLine("  /notBasicV       Disallow BASIC V assembler (overrides auto-detection)");
            Console.WriteLine("  /addnumbers      Supply missing line numbers (Z80 only)");
            Console.WriteLine("  /align           Right-align line numbers");
            Console.WriteLine("  /indent          Indent listing of loops and subprocedures");
            Console.WriteLine("  /indent=(loops | defs | all)");
            Console.WriteLine("                   Indent loops only | PROC & FN definitions | both");
            Console.WriteLine("  /nonumbers       Omits line numbers");
            Console.WriteLine("  /noformat        List program as entered (cancels prettyprint, splitlines and all additional spaces)");
            Console.WriteLine("  /columns         Format assembly language listings into columns");
            Console.WriteLine("  /columns=<extra> Format assembler into columns. <extra> must be -5 to 20 inc.");
            Console.WriteLine("  /bare            Omits additional messages (cancels pause)");
            Console.WriteLine("  /splitlines      Prints each statement on its own line");
            Console.WriteLine("  /pause           Pause at bottom of each screenful");
            Console.WriteLine("  /prettyprint     Adds spaces and syntax colouring");
            Console.WriteLine("  /cls             Clear console (terminal) before listing");
            Console.WriteLine("  /dark            Dark mode – black background (default)");
            Console.WriteLine("  /light           Light mode – white background");
            Console.WriteLine("\nE.g.");
            Console.WriteLine("  BasList program ,200        - List up to line 200");
            Console.WriteLine("  BasList program 1000,       - List from line 1000");
            Console.WriteLine("  BasList program 200,1000    - List from line 200 to 1000");
            Console.WriteLine("  BasList program 200 1000    - List from line 200 to 1000");
            Console.WriteLine("  BasList program IF PRINTTAB - List only lines containing PRINTTAB or PRINT TAB, case insensitive");
            Console.WriteLine("  BasList program IFX printer - As IF, but respecting spaces and case");
            Console.WriteLine("  BasList program LIST FNinp  - List named function(s)/procedure(s)");
            Console.WriteLine("\nOptions may be specified in any order, may start with / or - and can be abbreviated.");
            Console.WriteLine("Parameters containing spaces must be enclosed by double quotes.");
            Console.WriteLine("IF, IFX or LIST clauses must be at the end, after options. Multiple matches may be \nentered and BasList will list any line containing at least one of them.");
            Console.WriteLine("\nFor further help, see ReadMe.");
        }
        static void ResetAndExit(CommandSwitches s)
        {
            Console.ForegroundColor = s.ForeColor;
            Console.BackgroundColor = s.BackColor;
            Environment.Exit(0);
        }
        static void DBG(string msg)
        {
            //Console.WriteLine(msg);
        }
    }
}