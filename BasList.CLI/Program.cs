namespace BasList.CLI
{
    using BasTools.Core;
    using System.Diagnostics;
    using System.Reflection;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using static System.Runtime.InteropServices.JavaScript.JSType;

    //using System.Windows.Forms

    //***************** CommandSwitches *****************
    public class CommandSwitches
    {
        // switches for detokenisation
        private bool _basicV;
        private bool _notBasicV;
        // switches for formatting
        internal bool FlgAddNums;
        internal bool FlgIndent;
        internal bool FlgEmphDefs;
        internal bool Align;
        // switches for listing
        internal bool NoFormat;
        internal bool NoLineNumbers;
        internal bool Bare;
        internal bool SplitLines;
        internal bool AssemblerColumns;
        private int _columnWidth;
        internal bool Pretty;
        private bool _flgPause;
        // switches for filtering listings
        [JsonIgnore]
        internal int FromLine;
        [JsonIgnore]
        internal int ToLine;
        [JsonIgnore]
        internal bool FlgIf;
        [JsonIgnore]
        internal bool FlgIfX;
        [JsonIgnore]
        internal bool FlgList;
        [JsonIgnore]
        internal List<string> DirectiveParams;
        // switches for appearance
        internal bool Clear;
        internal bool FlgDark;
        // defaults
        [JsonIgnore]
        internal bool FlgSaveDefaults;
        [JsonIgnore]
        internal bool FlgResetDefaults;
        // debug
        [JsonIgnore]
        internal bool Debug;
        [JsonIgnore]
        internal bool FullDebug;
        [JsonIgnore]
        internal bool FlgExportTagged;
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
            FlgSaveDefaults = false;
            FlgResetDefaults = false;
            FlgExportTagged = false;
            DirectiveParams = new();
            Debug = false;
            FullDebug = false;
        }
        internal bool BasicV
        {
            get => _notBasicV ? false : _basicV;
            set => _basicV = value;
        }
        internal bool NotBasicV
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
        internal bool FlgPause
        {
            get => !Console.IsOutputRedirected && _flgPause;
            set => _flgPause = value;
        }
        public void checkFromTo()
        {
            if (FromLine > ToLine) (FromLine, ToLine) = (ToLine, FromLine); // swop using tuple
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
        public ListerOptions copyToListerOptions()
        {
            ListerOptions opts = new ListerOptions();

            // formatting
            opts.FlgAddNums = FlgAddNums;
            opts.FlgIndent = FlgIndent;
            opts.FlgEmphDefs = FlgEmphDefs;
            opts.Align = Align;

            // listing
            opts.NoFormat = NoFormat;
            opts.NoLineNumbers = NoLineNumbers;
            opts.Bare = Bare;
            opts.SplitLines = SplitLines;
            opts.AssemblerColumns = AssemblerColumns;
            opts.ColumnWidth = _columnWidth;
            opts.Pretty = Pretty;
            opts.FlgPause = _flgPause;

            // filtering
            opts.FromLine = FromLine;
            opts.ToLine = ToLine;
            opts.FlgIf = FlgIf;
            opts.FlgIfX = FlgIfX;
            opts.FlgList = FlgList;
            opts.DirectiveParams = DirectiveParams;

            // appearance
            opts.Clear = Clear;
            opts.FlgDark = FlgDark;

            // debug
            opts.Debug = Debug;
            opts.FullDebug = FullDebug;
            opts.FlgExportTagged = FlgExportTagged;

            return opts;
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

            //******** read Defaults ********
            string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDir = Path.Combine(baseDir, "RowlandSoftware");
            string appDefaultsFile = Path.Combine(appDir, "BasListPrefs.json");

            if (File.Exists(appDefaultsFile))
            {
                string json = File.ReadAllText(appDefaultsFile);
                CommandSwitchesSerializer.Deserialize(switches, json);
            }

            //******** readCommandSwitches ********

            readCommandSwitches(args, switches, ref filename);

            // Defaults
            
            if (switches.FlgResetDefaults)
            {
                if (Directory.Exists(appDir))
                    if (File.Exists(appDefaultsFile))
                        File.Delete(appDefaultsFile);
                Environment.Exit(0);
            }
            if (switches.FlgSaveDefaults)
            {
                if (!Directory.Exists(appDir))
                    Directory.CreateDirectory(appDir);

                string json = CommandSwitchesSerializer.Serialize(switches);
                File.WriteAllText(appDefaultsFile, json);
            }

            // Show message
            Console.Error.WriteLine("Processing, please wait...");

            BasToolsEngine engine = new BasToolsEngine();
            bool flgZ80 = false;
            ProgInfo progInfo = new(flgZ80, switches.BasicV, filename);
            FormattingOptions formatOptions = switches.copyToFormatOptions();
            ListerOptions listerOptions = switches.copyToListerOptions();

            try
            {
                Listing? formattedListing = null;
                if (engine.LoadAndFormatFile(filename, formatOptions, progInfo))
                {
                    formattedListing = engine.CurrentListing;
                    //Console.WriteLine($"I got {formattedListing.Lines.Count} lines");
                    BasLister.DisplayProgramLines(formattedListing, listerOptions, progInfo);
                }
            }
            catch (BasToolsException ex)
            {
                Console.Error.WriteLine($"{ex.Message}\n\n{ex.InnerException?.Message ?? ""}");
            }
        }

        //**************** Get User Input *****************
        static void readCommandSwitches(string[] args, CommandSwitches switches,ref string filename)
        {
            foreach (string arg in args)
            {
                bool recognised = false;
                if (arg.StartsWith('/') && arg.Length > 1)
                {
                    string arg2 = arg.Substring(1).ToUpperInvariant(); // remove the /
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
                    bool flgNegative = arg2.StartsWith('-');
                    if (flgNegative)
                        arg2 = arg2.Substring(1);
                    
                    if (arg2 == "V") { switches.BasicV = !flgNegative; recognised = true; }
                    if ("NOTBASICV".StartsWith(arg2)) { switches.NotBasicV = !flgNegative; recognised = true; }
                    if (arg2 == "?" || "HELP".StartsWith(arg2)) { help(); Environment.Exit(0); }
                    if ("ADDNUMBERS".StartsWith(arg2)) { switches.FlgAddNums = !flgNegative; recognised = true; }
                    if ("BARE".StartsWith(arg2)) { switches.Bare = !flgNegative; recognised = true; }
                    if ("SPLITLINES".StartsWith(arg2)) { switches.SplitLines = !flgNegative; recognised = true; }
                    if ("PAUSE".StartsWith(arg2)) { switches.FlgPause = !flgNegative; recognised = true; }
                    if ("PRETTYPRINT".StartsWith(arg2)) { switches.Pretty = !flgNegative; recognised = true; }
                    if ("ALIGN".StartsWith(arg2)) { switches.Align = !flgNegative; recognised = true; }
                    if ("COLUMNS".StartsWith(arg2)) { switches.AssemblerColumns = !flgNegative; recognised = true; }
                    if ("CLS".StartsWith(arg2)) { switches.Clear = !flgNegative; recognised = true; }
                    if ("CLEAR".StartsWith(arg2)) { switches.Clear = !flgNegative; recognised = true; }
                    if ("INDENT".StartsWith(arg2)) { switches.FlgIndent = !flgNegative; switches.FlgEmphDefs = !flgNegative; recognised = true; }
                    if ("NONUMBERS".StartsWith(arg2)) { switches.NoLineNumbers = !flgNegative; recognised = true; }
                    if ("NOFORMAT".StartsWith(arg2)) { switches.NoFormat = !flgNegative; recognised = true; }
                    if ("DARK".StartsWith(arg2)) { switches.FlgDark = !flgNegative; recognised = true; }
                    if ("LIGHT".StartsWith(arg2)) { switches.FlgDark = flgNegative; recognised = true; }
                    if ("DEBUG".StartsWith(arg2)) { switches.Debug = true; recognised = true; }
                    if ("FULLDEBUG".StartsWith(arg2)) { switches.FullDebug = true; recognised = true; }
                    if ("SAVEDEFAULTS".StartsWith(arg2)) { switches.FlgSaveDefaults = true; recognised = true; }
                    if ("RESETDEFAULTS".StartsWith(arg2)) { switches.FlgResetDefaults = true; recognised = true; }
                    if ("EXPORTTAGGED".StartsWith(arg2)) { switches.FlgExportTagged = true; recognised = true; }
                    if (!recognised && !switches.Bare) Console.Error.WriteLine("Option " + arg.ToLowerInvariant() + " not recognised");
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
                    catch (System.FormatException)
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
            !args[i].Equals("IF", StringComparison.OrdinalIgnoreCase) &&
            !args[i].Equals("IFX", StringComparison.OrdinalIgnoreCase) &&
            !args[i].Equals("LIST", StringComparison.OrdinalIgnoreCase))
            { i++; }

            // Move past the directive keyword
            if (i < args.Length) i++;

            for (int j = i; j < args.Length; j++)
                s.DirectiveParams.Add(args[j]);
        }
        public static class CommandSwitchesSerializer
        {
            public static string Serialize(CommandSwitches obj)
            {
                var dict = new Dictionary<string, object?>();

                var fields = typeof(CommandSwitches)
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var f in fields)
                {
                    if (f.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                        continue;

                    dict[f.Name] = f.GetValue(obj);
                }

                return JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            }

            public static void Deserialize(CommandSwitches obj, string json)
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (dict == null) return;

                var fields = typeof(CommandSwitches)
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var f in fields)
                {
                    if (f.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                        continue;

                    if (!dict.TryGetValue(f.Name, out var elem))
                        continue;

                    object? value = elem.Deserialize(f.FieldType);
                    f.SetValue(obj, value);
                }
            }
        }


        static void help()
        {
            string vs = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion ?? "1.1.0"; // ?? = null coalescing operator. //requires ref to System.Windows.Forms

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nBasList vs {vs} for BasTools (C) Andrew Rowland 2022-26");
            Console.WriteLine("Lists a BBC BASIC program file\n");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("    BasList [/file=]filename ([[from line] [to line]) | [line,line]]) [Options] ([IF ...] | [IFX ...] | [LIST ...])");
            //Console.WriteLine("BasList [/file=]filename [/V] [/addnumbers] [/align] [/indent] [/nonumbers] [/noformat] [/bare] [/pause] [/prettyprint] [(cls | clear)]");
            //Console.WriteLine("BasList [/file=]filename [(/dark | /light)]");
            Console.WriteLine("    BasList [/? | /h]  Display help\n");
            Console.WriteLine("    [/file=]filename  Specifies filename of tokenised BASIC program.");
            Console.WriteLine("                      Filename to follow '=' without spaces. Quote if contains spaces.");
            Console.WriteLine("                      '/file=' may be omitted if filename is first item");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("    OPTIONS");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("      /V               Interpret BASIC V assembler (May be auto-detected)");
            Console.WriteLine("      /notBasicV       Disallow BASIC V assembler (overrides auto-detection)");
            Console.WriteLine("      /addnumbers      Supply missing line numbers (Z80 only)");
            Console.WriteLine("      /align           Right-align line numbers");
            Console.WriteLine("      /indent          Indent listing of loops and subprocedures");
            Console.WriteLine("      /indent=(loops | defs | all)");
            Console.WriteLine("                       Indent loops only | PROC & FN definitions | both");
            Console.WriteLine("      /nonumbers       Omits line numbers");
            Console.WriteLine("      /noformat        List program as entered (cancels prettyprint, splitlines and all additional spaces)");
            Console.WriteLine("      /columns         Format assembly language listings into columns");
            Console.WriteLine("      /columns=<extra> Format assembler into columns. <extra> must be -5 to 20 inc.");
            Console.WriteLine("      /bare            Omits additional messages (cancels pause)");
            Console.WriteLine("      /splitlines      Prints each statement on its own line");
            Console.WriteLine("      /pause           Pause at bottom of each screenful");
            Console.WriteLine("      /prettyprint     Adds spaces and syntax colouring");
            Console.WriteLine("      /cls             Clear console (terminal) before listing");
            Console.WriteLine("      /dark            Dark mode – black background (default)");
            Console.WriteLine("      /light           Light mode – white background");
            Console.WriteLine("      /savedefaults    Save current switches as defaults");
            Console.WriteLine("      /resetdefaults   Clear saved defaults back to application defaults");
            Console.WriteLine("      /debug           Display internal detokenised results for debug");
            Console.WriteLine("      /fulldebug       Debug with additional information");
            Console.WriteLine("      /exporttagged    Display with syntax tags only");
            Console.WriteLine("\n    E.g.");
            Console.WriteLine("      BasList program ,200        - List up to line 200");
            Console.WriteLine("      BasList program 1000,       - List from line 1000");
            Console.WriteLine("      BasList program 200,1000    - List from line 200 to 1000");
            Console.WriteLine("      BasList program 200 1000    - List from line 200 to 1000");
            Console.WriteLine("      BasList program IF PRINTTAB - List only lines containing PRINTTAB or PRINT TAB, case insensitive");
            Console.WriteLine("      BasList program IFX printer - As IF, but respecting spaces and case");
            Console.WriteLine("      BasList program LIST FNinp  - List named function(s)/procedure(s)");
            Console.WriteLine("\nOptions may be specified in any order and can be abbreviated.");
            Console.WriteLine("\nTo override a saved value, use a minus sign e.g. /-align.");
            Console.WriteLine("Parameters containing spaces must be enclosed by double quotes.");
            Console.WriteLine("IF, IFX or LIST clauses must be at the end, after options. Multiple matches may be \nentered and BasList will list any line containing at least one of them.");
            Console.WriteLine("\nFor further help, see ReadMe.");
        }
        static bool IsNumeric(string s) // special for decoding line numbers - includes comma
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (!"0123456789,".Contains(s.Substring(i, 1))) return false;
            }
            return true;
        }
        static void DBG(string msg)
        {
            //Console.WriteLine(msg);
        }
    }
}