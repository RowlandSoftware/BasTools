namespace BasList.CLI
{
    using BasTools.Core;
    using System.Collections;
    using System.Diagnostics;
    using System.Reflection;
    using static System.Runtime.InteropServices.JavaScript.JSType;
    using static System.Windows.Forms.AxHost;
    using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

    //***************** CommandSwitches *****************
    public class CommandSwitches
    {
        public bool BasicV;
        public bool FlgAddNums;
        public bool FlgIndent;
        public bool Align;
        public bool NoSpaces;
        public bool NoLineNumbers;
        public bool Bare;
        public bool BreakApart;
        public bool Pretty;
        public bool FlgIf;
        public bool FlgIfX;
        public bool FlgList;
        private bool _flgPause;
        public bool FlgDark;
        public int FromLine;
        public int ToLine;

        public ConsoleColor ForeColor;
        public ConsoleColor BackColor;

        public List<string> DirectiveParams;
        public CommandSwitches()
        {
            BasicV = false;
            FlgAddNums = false;
            FromLine = 0;
            ToLine = -1;
            FlgIndent = false;
            Align = false;
            NoSpaces = false;
            NoLineNumbers = false;
            Bare = false;
            BreakApart = false;
            Pretty = false;
            FlgIf = false;
            FlgIfX = false;
            FlgList = false;
            FlgPause = false;
            FlgDark = true;
            DirectiveParams = new();
        }
        // Deep copy so lookahead doesn't mutate the real list
        //DirectiveParams = new List<string>(other.DirectiveParams);

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
            [SemanticTags.StrongKeyword] = ConsoleColor.Blue,
            [SemanticTags.StringLiteral] = ConsoleColor.Green,
            [SemanticTags.Variable] = ConsoleColor.Magenta,
            [SemanticTags.RemText] = ConsoleColor.Yellow,
            [SemanticTags.AssemblerComment] = ConsoleColor.Yellow,
            [SemanticTags.EmbeddedData] = ConsoleColor.White,
            [SemanticTags.ProcFunction] = ConsoleColor.Cyan,
            [SemanticTags.Label] = ConsoleColor.Magenta,
            [SemanticTags.Register] = ConsoleColor.Green,
            [SemanticTags.Mnemonic] = ConsoleColor.Blue,
            [SemanticTags.Operator] = ConsoleColor.Red,
            [SemanticTags.LineNumber] = ConsoleColor.Gray
        };
        public static bool TryGetColor(string tag, out ConsoleColor color, bool darkMode)
        {
            if (_map.TryGetValue(tag, out color))
            {
                if (color == ConsoleColor.Yellow && !darkMode)
                    color = ConsoleColor.DarkYellow;

                return true;
            }
            return false;
        }
    }
    class ListerState
    {
        public int LineCount;
        public bool Printme;
        public int Indent;
        public bool Listme;
        public ListerState()
        {
            LineCount = 0;
            Printme = false;
            Indent = 0;
            Listme = false;
        }
        public ListerState(ListerState other)
        {
            LineCount = other.LineCount;
            Printme = other.Printme;
            Indent = other.Indent;
            Listme = other.Listme;
        }
    }
    public class Program
    {
        static void Main(string[] args)
        {
            CommandSwitches switches = new();
            string filename = string.Empty;
            string format = string.Empty;

            readCommandSwitches(args, switches, ref filename, ref format);
            //List<string> listing = new();
            // now we've loaded the file, show message
            Console.Error.WriteLine("Processing, please wait...");

            BasToolsEngine engine = new BasToolsEngine();
            bool flgZ80 = false;

            Listing listing = new(new List<ProcessedLine>(), new List<Token>());
            engine.Process(filename, ref flgZ80, switches.BasicV, listing);

            displayProgramLines(listing, switches, filename, flgZ80);
        }

        //**************** Get User Input *****************
        static void readCommandSwitches(string[] args, CommandSwitches switches, ref string fn, ref string format)
        {
            try
            {
                foreach (string arg in args)
                {
                    bool recognised = false;
                    if ((arg.StartsWith('/') || arg.StartsWith('-')) && arg.Length > 1)
                    {
                        string arg2 = arg.Substring(1).ToUpper();
                        if (arg2 == "V") { switches.BasicV = true; recognised = true; }
                        if ("ADDNUMBERS".StartsWith(arg2)) { switches.FlgAddNums = true; recognised = true; }
                        if (arg2 == "?" || arg2 == "H") { help(); Environment.Exit(0); }
                        if ("BARE".StartsWith(arg2)) { switches.Bare = true; recognised = true; }
                        if ("BREAKAPART".StartsWith(arg2)) { switches.BreakApart = true; recognised = true; }
                        if ("PAUSE".StartsWith(arg2)) { switches.FlgPause = true; recognised = true; }
                        if (arg2.Contains('='))
                        {
                            string arg1 = arg2.Substring(0, arg2.IndexOf('='));

                            if ("FILE".StartsWith(arg1))
                            {
                                fn = arg2.Substring(arg2.IndexOf('=') + 1);
                                recognised = true;
                            }
                        }
                        if ("PRETTYPRINT".StartsWith(arg2)) { switches.Pretty = true; recognised = true; }
                        if ("ALIGN".StartsWith(arg2)) { switches.Align = true; recognised = true; }
                        if ("INDENT".StartsWith(arg2)) { switches.FlgIndent = true; recognised = true; }
                        if ("NONUMBERS".StartsWith(arg2)) { switches.NoLineNumbers = true; recognised = true; }
                        if ("NOSPACES".StartsWith(arg2)) { switches.NoSpaces = true; recognised = true; }
                        if ("DARK".StartsWith(arg2)) { switches.FlgDark = true; recognised = true; }
                        if ("LIGHT".StartsWith(arg2)) { switches.FlgDark = false; recognised = true; }
                        if (!recognised && !switches.Bare) Console.Error.WriteLine("Option " + arg.ToLower() + " not recognised");
                    }
                    // not a switch ...
                    if (IsNumeric(arg))
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
                    else if (fn.Length == 0 && !recognised) // This is where we pick up the filename if not already found
                    {
                        if (arg != "IF" && arg != "IFX" && arg != "LIST")
                            fn = arg;
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
                if (switches.FlgList) { switches.FromLine = 0; switches.ToLine = 0xFFFF; } // line numbers ignored for LIST
            }
            catch (System.FormatException fe)
            {
                Console.Error.WriteLine("Line numbers not in correct format");
                //Console.WriteLine(fe.Message);
                Environment.Exit(0);
            }
            if (switches.ToLine < 0) switches.ToLine = 0xffff;

            // no filename found:
            if (fn.Length == 0)
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
        private static void displayProgramLines(Listing listing, CommandSwitches switches, string filename, bool flgZ80)
        {
            switches.BackColor = ConsoleColor.Black;
            switches.ForeColor = ConsoleColor.White;
            switches.SwopIfLight();

            Console.ForegroundColor = switches.ForeColor;
            Console.BackgroundColor = switches.BackColor;
            //Console.Clear();
            int linesprinted = 0;

            //
            // ******** LISTING STARTS HERE ********
            //
            string format = flgZ80 ? "Z80" : "Acorn";
            if (!switches.Bare && !switches.FlgList) Console.WriteLine($"\nListing {filename} from line {switches.FromLine} to {switches.ToLine} ({format} format)\n");

            string line;
            string linenospaces;
            string linenumber;
            string sIndent = string.Empty;
            ListerState State = new();      // this sets initial conditions

            foreach (ProcessedLine progline in listing.ProgramLines)
            {
                linenumber = progline.LineNumber.ToString();
                if (progline.LineNumber == 0 && flgZ80)
                {
                    linenumber = string.Empty;
                    if (switches.FlgAddNums) linenumber = (State.LineCount * 10).ToString();
                }
                else if (switches.Align)
                    linenumber = linenumber.PadLeft(5);
                if (switches.Pretty) linenumber = SemanticTags.LineNumber + linenumber + SemanticTags.Reset;
                if (!switches.NoSpaces)
                {
                    if (linenumber != string.Empty) linenumber+= " "; // only space if number present
                    sIndent = "".PadLeft(State.Indent * 2, ' ');
                }
            }
        }
        /**** ----------------  ******/
        /*static void PrettyPrint(string msg, ParserState state, EngineSwitches switches)
       {
           string[] ss = msg.Split('{', '}');
           foreach (var s in ss)
           {
               if (s.Equals("/"))
               {
                   //Console.ResetColor();
                   Console.BackgroundColor = switches.BackColor;
                   Console.ForegroundColor = switches.ForeColor;
               }
               else
               {
                   try
                   {
                       if (s.StartsWith('=') && Enum.TryParse(s.Substring(1), out ConsoleColor c))
                           Console.ForegroundColor = c;
                       else
                           Console.Write(s);
                   }
                   catch (Exception e)
                   {
                       Console.Write(s);
                   }
               }
           }
           Console.WriteLine("");
       }
       private bool isEndOfProc(ParserState s, EngineSwitches switches) // s is a CLONE so can use freely
       {
           string templine = getNextLine(s, switches);

           if (templine == null) return true; // End of program

           templine = Regex.Replace(templine, @"\{.*?\}", "");
           templine = templine.Replace(" ", "").ToUpper();
           templine = templine.Replace(":", "");
           templine = Regex.Replace(templine, @"REM.*$", "", RegexOptions.IgnoreCase);

           if (templine.Length == 0) return isEndOfProc(s, switches);

           return templine.StartsWith("DEF");
       }
       private string getNextLine(ParserState s, EngineSwitches switches)
       {
           // End of program?
           if (!s.Z80 && ((switches.BasicV && s.Data[s.Ptr + 1] == 255) || s.Data[s.Ptr + 1] > 127)) return null;
           if (s.Z80 && s.Data[s.Ptr + 2] == 255 && s.Data[s.Ptr + 3] == 255) return null;

           // compute Bound for the clone (saved in s.Bound)
           int dummy = GetLineNumber(s, switches);

           processLineBody(s, switches, out string line);

           return line;
       }
       static void getDirectiveParams(string[] args, ParserState s)
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
       //**** Utility functions ******
       private static void ClearCurrentConsoleLine()
       {
           int currentLineCursor = Console.CursorTop;
           Console.SetCursorPosition(0, Console.CursorTop);
           for (int i = 0; i < Console.WindowWidth; i++)
               Console.Write(" ");
           Console.SetCursorPosition(0, currentLineCursor);
       }/*
       static bool IsNumeric(string s)
       {
           for (int i = 0; i < s.Length; i++)
           {
               if (!"0123456789,".Contains(s.Substring(i, 1))) return false;
           }
           return true;
       }*/
        /*private bool nameMatch(int ptr, ParserState s, EngineSwitches switches)
        {
            if (!switches.FlgList) return false;

            string procName = readProcFnName(++ptr, s, switches);
            if (procName == null) return false;

            for (int i = 0; i < s.DirectiveParams.Count; i++)
            {
                //Console.WriteLine($"Matching {procName} - {s.DirectiveParams[i]}");
                if (procName.Equals(s.DirectiveParams[i], StringComparison.InvariantCultureIgnoreCase)) return true;
            }
            return false;
        }
        private string readProcFnName(int ptr, ParserState s, EngineSwitches switches)
        {
            while (s.Data[ptr] == 32) ptr++; // this is correct: leave ptr pointing at first non-space char

            char dummy = ' ';
            string result = getKeywordOrLineNumber(s.Data[ptr], ref ptr, ref dummy, s, switches);

            if (result == null) return null;
            if (result != "FN" && result != "PROC") throw new Exception("Invalid DEF"); // return null;

            ptr++;
            result += readNextToken(ptr, s);
            //Console.WriteLine(result);
            return result;
        }
        /**** Utility functions ******/
        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int i = 0; i < Console.WindowWidth; i++)
                Console.Write(" ");
            Console.SetCursorPosition(0, currentLineCursor);
        }
        static bool IsNumeric(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (!"0123456789,".Contains(s.Substring(i, 1))) return false;
            }
            return true;
        }
        static void help()
        {
            string vs = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion; //"1.0.4";
                                                                                                              //string vs = Assembly.Application.FileVersion; // requires ref to System.Windows.Forms
            Console.WriteLine($"\nBasList vs {vs} (C) Andrew Rowland 2022-26");
            Console.WriteLine("\nLists a BBC BASIC program file\n");
            Console.WriteLine("BasList [/file=]filename ([[from line] [to line]) | [line,line]]) [Options] ([IF ...] | [IFX ...] | [LIST ...])");
            //Console.WriteLine("BasList [/file=]filename [/V] [/addnumbers] [/align] [/indent] [/nonumbers] [/nospaces] [/bare] [/pause] [/prettyprint]");
            //Console.WriteLine("BasList [/file=]filename [/mode=(dark | light | none)]");
            Console.WriteLine("BasList /?     Display help\n");
            Console.WriteLine("  [/file=]filename");
            Console.WriteLine("               Specifies filename of tokenised BASIC program.");
            Console.WriteLine("               Filename to follow '=' without spaces. Quote if contains spaces.");
            Console.WriteLine("               '/file=' may be omitted if filename is first item");

            Console.WriteLine("\nOPTIONS");
            Console.WriteLine("-------");
            Console.WriteLine("  /V           Allow BASIC V keywords");
            Console.WriteLine("  /addnumbers  Supply missing line numbers (Z80 only)");
            Console.WriteLine("  /align       Right-align line numbers");
            Console.WriteLine("  /indent      Indent listing of loops (unless Nospaces specified)");
            Console.WriteLine("  /nonumbers   Omits line numbers");
            Console.WriteLine("  /nospaces    Omits spaces and indent after line numbers");
            Console.WriteLine("  /bare        Omits additional messages (cancels pause)");
            Console.WriteLine("  /breakapart  Prints each statement on its own line");
            Console.WriteLine("  /pause       Pause at bottom of each screenful");
            Console.WriteLine("  /prettyprint Adds spaces and syntax colouring");
            Console.WriteLine("  /dark        Dark mode – black background (default)");
            Console.WriteLine("  /light       Light mode – white background");
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
            Console.WriteLine("IF, IFX or LIST clauses must be at the end, after options. Multiple matches may be\nentered and BasList will list any line containing at least one of them.");
            Console.WriteLine("\nFor further help, see ReadMe.");
        }
        static void ResetAndExit(CommandSwitches s)
        {
            Console.ForegroundColor = s.ForeColor;
            Console.BackgroundColor = s.BackColor;
            Environment.Exit(0);
        }
    }
}