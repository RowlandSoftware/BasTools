using BasAnalysis.CLI;
using BasTools.Core;
using System.Globalization;
using Windows.Media.Playback;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace BasAnalysis.CLI
{
    public class Program
    {
        enum SymbolKind
        {
            StaticInt,
            IntVar,
            RealVar,
            StringVar,
            RealArray,
            IntArray,
            StringArray,
            LiteralString,
            Fn,
            Proc,
            Label,
            FuckKnows
        }
        enum SymbolReadOrWrite
        {
            Assigned,
            Referenced
        }
        enum SymbolContext
        {
            Global,
            Local,
            Parameter,
            Call,
            TBD
        }
        enum ProcedureType
        {
            Proc,
            Fn,
            Root
        }
        class SymbolUse
        {
            public int LineNumber { get; init; }
            public SymbolContext symbolContext { get; init; }       // Global, Local, Parameter, Call
            public SymbolReadOrWrite symbolReadWrite { get; init; }   // Assigned / Referenced
            public string? ParentProcOrFn { get; set; }             // null = global
            public ProcedureType? ParentProcedureType { get; set; } // null = global
        }
        class SymbolInfo
        {
            public string Name { get; init; } = "";
            public SymbolKind Kind { get; init; }   // IntVar, RealVar, StringVar, Array, PROC, FN, LocalVar, etc.

            public int AssignedCount { get; set; }
            public int ReferencedCount { get; set; }

            public List<SymbolUse> Uses { get; } = new();   // line numbers, contexts, parent
        }

        static Dictionary<string, SymbolInfo> Symbols = new();
        static bool analyzed; // = false by default

        static void Main(string[] args)
        {
            string cmd = string.Empty;
            string prompt = "BasAnalysis >";

            BasToolsEngine engine = new BasToolsEngine();
            ProgInfo BAprogInfo = new();
            FormattingOptions formatOptions = new();

            Utilities.banner();

            if (args.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Enter LOAD <filename> or HELP for assistance");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                load(args[0], BAprogInfo, engine, ref prompt);
            }
            //
            // Main Loop
            //
            while (cmd != "QUIT" && cmd != "EXIT" && cmd != "END" && cmd != "X")
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();
                if (input == null || input.IsWhiteSpace()) continue;

                string[] arglist = Utilities.SplitArgList(input);
                cmd = arglist[0].ToUpper(CultureInfo.InvariantCulture);

                if (cmd.EndsWith('.'))
                {
                    string abbrev = cmd.Substring(0, cmd.Length - 1);
                    string[] commands = { "HELP", "LIST", "LISTIF", "LOAD", "ANALYZE", "ANALYSE", "CLEAR", "CLS", "LVAR", "LVARS", "LFN", "LPROC", "TREE", "PREVIEW", "EXIT", "END", "QUIT" };
                    // "LISTIF", "LISTIFX", "BLIST"
                    foreach (string match in commands)
                    {
                        if (match.StartsWith(abbrev, StringComparison.OrdinalIgnoreCase))
                        {
                            cmd = match;
                            break;
                        }
                    }
                }

                switch (cmd)
                {
                    case "?":
                    case "-H":
                    case "HELP":
                        Utilities.help(arglist);
                        break;
                    case "LOAD":
                        if (arglist.Length < 2)
                            Utilities.help(new string[] { "", "LOAD" });
                        else
                            load(arglist[1], BAprogInfo, engine, ref prompt);
                        break;
                    case "CLS":
                    case "CLEAR":
                        Console.Clear();
                        break;
                    case "PREVIEW":
                        Preview(engine); break;
                    case "LIST":
                        ListProg(engine, arglist); break;
                    case "LISTIF":
                        ListIf(engine, arglist); break;
                    case "ANALYZE":
                    case "ANALYSE":
                        Analyse(engine, ref analyzed);
                        break;
                    case "LVAR":
                        Listvar(arglist, engine, analyzed);
                        break;
                    case "LVARS":
                        Listvars(arglist, engine, analyzed)
                        ; break;
                    case "QUIT":
                    case "EXIT":
                    case "END":
                    case "X":
                        break;
                    default:
                        Console.WriteLine($"'{cmd}' not recognised");
                        break;
                }
            }
        }
        static bool load(string filename, ProgInfo BAprogInfo, BasToolsEngine engine, ref string prompt)
        {
            try
            {
                FormattingOptions formatOptions = new FormattingOptions
                {
                    Align = true,
                    AssemblerColumns = true,
                    FlgAddNums = true,
                    FlgIndent = true
                };

                engine.loadAndFormatFile(filename, formatOptions, BAprogInfo);

                prompt = "BasAnalysis " + Path.GetFileName(filename) + " >";

                Symbols.Clear();
                analyzed = false;

                Console.WriteLine($"Program loaded. {BAprogInfo.NumberOfLines} lines, {BAprogInfo.LengthInBytes} bytes " +
                $"(&{BAprogInfo.LengthInBytes:X4}), {BAprogInfo.LengthInBytes / 1024.0:F2} KB" +
                $"\n\nEnter 'preview' to see first 20 lines, or enter 'analyse'\n");

                return true;
            }
            catch (BasToolsException e)
            {
                Console.WriteLine($"{e.Message}");
            }
            return false;
        }
        static bool Analyse(BasToolsEngine engine, ref bool analyzed)
        {
            if (engine.CurrentListing == null)
            {
                Console.WriteLine("ANALYSE - No program loaded.");
                return false;
            }

            bool expectingAssignmentTarget;
            ProcedureType procedureType = ProcedureType.Root;
            string procedureName = string.Empty;

            foreach (ProgramLine line in engine.CurrentListing.Lines)
            {
                // line level state
                expectingAssignmentTarget = true; // at start of statement...

                //Console.Write($"{line.LineNumber} ");
                List<Token> tokens = BasToolsEngine.WalkTagged(line.TaggedLine).ToList();

                for (int i = 0; i < tokens.Count; i++)
                {
                    SymbolContext context = SymbolContext.TBD;
                    var tok = tokens[i];

                    if (tok.tag == SemanticTags.StatementSep)
                    {
                        expectingAssignmentTarget = true;
                        continue;
                    }

                    if (tok.tag == SemanticTags.Variable)
                    {
                        // Look ahead to see if this is an assignment
                        Token? next = PeekNextNonSpaceToken(tokens, i);

                        bool isAssignment = expectingAssignmentTarget &&
                                            next?.tag == SemanticTags.Operator &&
                                            next?.value == "=";

                        RecordUse(tok.tag, tok.value, line.LineNumber,
                            isAssignment ? SymbolReadOrWrite.Assigned : SymbolReadOrWrite.Referenced,
                            SymbolContext.TBD, procedureName, procedureType);

                        expectingAssignmentTarget = false;
                        continue;
                    }

                    if (tok.tag is SemanticTags.ProcName or SemanticTags.FunctionName)
                    {
                        RecordUse(tok.tag, tok.value, line.LineNumber,
                            line.IsDef ? SymbolReadOrWrite.Assigned : SymbolReadOrWrite.Referenced,
                            line.IsDef ? SymbolContext.TBD : SymbolContext.Call, procedureName, procedureType);

                        continue;
                    }
                    if (tok.tag is SemanticTags.StringLiteral)
                    {
                        RecordUse(tok.tag, tok.value, line.LineNumber,
                            SymbolReadOrWrite.Assigned,
                            SymbolContext.TBD, procedureName, procedureType);

                        expectingAssignmentTarget = false;
                        continue;
                    }
                    if (tok.tag is SemanticTags.Label)
                    {
                        RecordUse(tok.tag, tok.value, line.LineNumber,
                            SymbolReadOrWrite.Assigned,
                            SymbolContext.TBD, procedureName, procedureType);

                        expectingAssignmentTarget = false;
                        continue;
                    }

                    if (tok.tag == SemanticTags.Operator && tok.value == "=")
                    {
                        // After '=', everything is a reference
                        expectingAssignmentTarget = false;
                        continue;
                    }

                    if (line.IsInDef == false) // exiting from PROC or FN
                    {
                        procedureType = ProcedureType.Root;
                    }
                    if (tok.tag == SemanticTags.Keyword && tok.value == "DEF") // entering PROC or FN
                    {
                        Token? next = PeekNextNonSpaceToken(tokens, i);
                        if (next?.tag == SemanticTags.ProcName)
                        {
                            procedureName = next.value;
                            switch (next.tag)
                            {
                                case SemanticTags.ProcName:
                                    procedureName = next.value;
                                    procedureType = ProcedureType.Proc;
                                    break;
                                case SemanticTags.FunctionName:
                                    procedureName = next.value;
                                    procedureType = ProcedureType.Fn;
                                    break;
                                default:
                                    procedureType = ProcedureType.Root;
                                    break;
                            }
                        }
                    }

                }
            }
            Console.Write($"Analysed {Symbols.Count} unique tokens\n");
            analyzed = true;
            return true;
        }
        private static Token? PeekNextNonSpaceToken(List<Token> tokens, int index)
        {
            for (int i = index + 1; i < tokens.Count; i++)
            {
                var t = tokens[i];

                // Skip null-tag whitespace tokens
                if (t.tag == null) // && string.IsNullOrWhiteSpace(t.value))
                    continue;

                return t;
            }

            return null;
        }
        static void Listvar(string[] arglist, BasToolsEngine engine, bool analyzed)
        {
            if (engine.CurrentListing == null)
            {
                Console.WriteLine("LVAR - No program loaded.");
                return;
            }
            if (!analyzed)
            {
                Console.WriteLine($"LVAR - Program '{engine.CurrentProgInfo.ProgName}' has not been analysed.");
                return;
            }

            // Find variable
            for (int j = 1; j < arglist.Length; j++)
            {
                string arg = arglist[j];

                foreach (SymbolInfo symInfo in Symbols.Values.Where(s => s.Kind is SymbolKind.StaticInt
                or SymbolKind.IntVar
                or SymbolKind.RealVar
                or SymbolKind.StringVar))
                {
                    if (arg == symInfo.Name)
                        VarDetail(arg, symInfo); // Console.WriteLine("{0,-20}{1,-10}{2,10}{3,11}", symInfo.Name, symInfo.Kind, symInfo.AssignedCount, symInfo.ReferencedCount);
                }
            }
        }
        static void Listvars(string[] arglist, BasToolsEngine engine, bool analyzed)
        {
            if (engine.CurrentListing == null)
            {
                Console.WriteLine("LVARS - No program loaded.");
                return;
            }
            if (!analyzed)
            {
                Console.WriteLine($"LVARS - Program '{engine.CurrentProgInfo.ProgName}' has not been analysed.");
                return;
            }

            // Static Integers
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  Static Integer Variables");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  {0,-20}{1,10}{2,11}\n", "Variable", "Assigned", "Referenced");
            PrintByKind(SymbolKind.StaticInt);

            Console.WriteLine("\n  Dynamic Variables (may include labels)");

            // Integers
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  Integer Variables");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  {0,-20}{1,10}{2,11}\n", "Variable", "Assigned", "Referenced");
            PrintByKind(SymbolKind.IntVar);

            // Real variables
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  Real Number Variables");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  {0,-20}{1,10}{2,11}\n", "Variable", "Assigned", "Referenced");
            PrintByKind(SymbolKind.RealVar);

            // String variables
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  String Variables");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  {0,-20}{1,10}{2,11}\n", "Variable", "Assigned", "Referenced");
            PrintByKind(SymbolKind.StringVar);

            // PROCs
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  Sub-procedures (PROCs)");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  {0,-20}{1,10}{2,11}\n", "PROC name", "Assigned", "Referenced");
            PrintByKind(SymbolKind.Proc);

            // FNs
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  Functions (FNs)");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  {0,-20}{1,10}{2,11}\n", "FN name", "Assigned", "Referenced");
            PrintByKind(SymbolKind.Fn);

            // Assembler label
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  Assembler labels");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  {0,-20}{1,10}{2,11}\n", "Label", "Assigned", "Referenced");
            PrintByKind(SymbolKind.Label);

            // Strings
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  Literal strings");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  {0,-35}{1,6}{2,10}\n", "String", "Count", "Length");
            PrintByKind(SymbolKind.LiteralString);
        }
        static void Preview(BasToolsEngine engine)
        {
            if (engine.CurrentListing == null)
            {
                Console.WriteLine("PREVIEW - No program loaded.");
                return;
            }
            List(engine, 0, 0xFEFF, 20);
        }
        static void ListProg(BasToolsEngine engine, string[] arglist)
        {
            if (engine.CurrentListing == null)
            {
                Console.WriteLine("LIST - No program loaded.");
                return;
            }
            int fromline = 0;
            int toline = 0xFFFF;

            if (arglist.Length > 1)
            {
                if (!int.TryParse(arglist[1], out fromline))
                {
                    ListDef(engine, arglist);
                    return;
                }
            }
            if (arglist.Length > 2)
            {
                if (!int.TryParse(arglist[2], out toline))
                    toline = 0xFFFF;
            }
            List(engine, fromline, toline, 0);
        }
        static void ListIf(BasToolsEngine engine, string[] arglist)
        {
            if (engine.CurrentListing == null)
            {
                Console.WriteLine("LISTIF - No program loaded.");
                return;
            }
            if (arglist.Length < 2)
            {
                Utilities.help(new string[] { "LISTIF", "listif" });
                return;
            }

            int linesprinted = 0;
            bool first = true;

            foreach(ProgramLine line in engine.CurrentListing.Lines)
            {
                foreach (string arg in arglist)
                {
                    if (first)
                    {
                        first = false;
                        continue;
                    }
                    if (line.NoSpacesLine.Contains(arg, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!Utilities.printLine(line, ref linesprinted)) return;
                        break; // only print once, even if more than one match
                    }
                }
            }
        }
        static void List(BasToolsEngine engine, int fromline, int toline, int totLineCount)
        {
            if (engine.CurrentListing == null)
            {
                Console.WriteLine("LIST - No program loaded.");
                return;
            }
            int linesprinted = 0;
            int linecount = 0;

            for (int i = 0; i < 0xFEFF && (totLineCount == 0 ? true : ++linecount <= totLineCount); i++)
            {
                if (i == engine.CurrentListing.Lines.Count)
                    return;

                ProgramLine progLine = engine.CurrentListing.Lines[i];

                if (progLine.LineNumber > toline) return;

                if (progLine.LineNumber >= fromline)
                {
                    if (!Utilities.printLine(progLine, ref linesprinted)) break;
                }
            }
        }
        static void ListDef(BasToolsEngine engine, string[] arglist)
        {
            if (engine.CurrentListing == null)
            {
                Console.WriteLine("No program loaded.");
                return;
            }

            for (int i = 1; i < arglist.Length; i++)
            {
                if (!(arglist[i].StartsWith("FN", StringComparison.OrdinalIgnoreCase) || arglist[i].StartsWith("PROC", StringComparison.OrdinalIgnoreCase)))
                {
                    Utilities.help(new string[] { "LIST", "list" });
                    Console.WriteLine("              i.e. List <FNname | PROCname> [<FNname | PROCname>] ...");
                    return;
                }
            }

            int linesprinted = 0;
            bool listme = false;

            for (int i = 0; i < engine.CurrentListing.Lines.Count; i++)
            {
                ProgramLine progLine = engine.CurrentListing.Lines[i];

                if (listme)
                {
                    if (!Utilities.printLine(progLine, ref linesprinted)) break;

                    listme = progLine.IsInDef;
                    if (!listme) Console.WriteLine("");
                    continue;
                }
                if (!progLine.IsDef)
                    continue;

                string name = BasToolsEngine.getTagValueFromLine(progLine.TaggedLine, SemanticTags.FunctionName);
                if (name != null)
                {
                    name = "FN" + name;
                }
                else
                {
                    name = BasToolsEngine.getTagValueFromLine(progLine.TaggedLine, SemanticTags.ProcName);
                    if (name != null)
                        name = "PROC" + name;
                    else continue;
                }
                bool first = true;
                foreach (string arg in arglist)
                {
                    if (first) // ignore the command as it could give dodgy results
                    {
                        first = false;
                        continue;
                    }
                    if (string.Equals(name, arg, StringComparison.OrdinalIgnoreCase))
                    {
                        listme = true;
                        Console.WriteLine("");
                        Utilities.printLine(progLine, ref linesprinted); // need to print out DEF line
                        break;
                    }
                }
            }
        }
        static void RecordUse(string tag, string name, int line, SymbolReadOrWrite readwrite, SymbolContext context, string currentProcName, ProcedureType procedureType)
        {
            //Console.WriteLine("{0,-10}{1,-10}{2,-10},{3,-10}", name, line, context, currentProcName);
            if (tag == SemanticTags.StringLiteral && string.IsNullOrWhiteSpace(name))
                return;

            SymbolKind kind = InferKind(tag, name);

            if (!Symbols.TryGetValue(kind + ":" + name, out var sym))
            {
                sym = new SymbolInfo { Name = name, Kind = kind };
                Symbols.Add(kind + ":" + name, sym);
            }

            if (readwrite == SymbolReadOrWrite.Assigned)
                sym.AssignedCount++;
            else
                sym.ReferencedCount++;

            SymbolUse symbolUse = new SymbolUse
            {
                LineNumber = line,
                symbolReadWrite = readwrite,
                symbolContext = context,
                ParentProcOrFn = currentProcName,
                ParentProcedureType = procedureType
            };
            sym.Uses.Add(symbolUse);
        }
        private static SymbolKind InferKind(string tag, string name)
        {
            if (name.EndsWith('%') && name.Length == 2 && (char.IsAsciiLetterUpper(name[0]) || name[0] == '@'))
                return SymbolKind.StaticInt;
            if (name.EndsWith('%'))
                return SymbolKind.IntVar;
            if (name.EndsWith('$'))
                return SymbolKind.StringVar;
            if (tag == SemanticTags.Variable)
                return SymbolKind.RealVar;
            if (tag == SemanticTags.FunctionName)
                return SymbolKind.Fn;
            if (tag == SemanticTags.ProcName)
                return SymbolKind.Proc;
            if (tag == SemanticTags.Label)
                return SymbolKind.Label;
            if (tag == SemanticTags.StringLiteral)
                return SymbolKind.LiteralString;
            return SymbolKind.FuckKnows;
            // TODO Arrays?
        }
        private static void PrintByKind(SymbolKind kind)
        {
            Console.ForegroundColor = ConsoleColor.White;
            bool alternate = true;
            foreach (SymbolInfo symInfo in Symbols.Values.Where(s => s.Kind == kind)
            .OrderBy(s => s.Name))
            {
                Console.ForegroundColor = alternate ? ConsoleColor.White : ConsoleColor.Gray;       // mild stripes
                alternate = !alternate;

                if (kind == SymbolKind.LiteralString)
                {
                    Console.WriteLine("  {0,-35}{1,6}{2,10} ", symInfo.Name, symInfo.AssignedCount, symInfo.Name.Length - 2);
                }
                else
                {
                    if (symInfo.ReferencedCount == 0) Console.ForegroundColor = ConsoleColor.Red;       // assigned but unused
                    if ((kind == SymbolKind.Fn || kind == SymbolKind.Proc) && symInfo.AssignedCount > 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;                                  // FN or PROC defined > once
                    }
                    Console.WriteLine("  {0,-20}{1,10}{2,11} ", symInfo.Name, symInfo.AssignedCount, symInfo.ReferencedCount);
                }
            }
            Console.ForegroundColor = ConsoleColor.White;
        }
        private static void VarDetail(string match, SymbolInfo symInfo)
        {
            SymbolKind varKind = InferKind("", match);
            if (symInfo.Kind != varKind) return;
            Console.WriteLine("  {0,10}: {1,20} - Assigned: {2,10} - Referenced :{3,11} ", symInfo.Kind, symInfo.Name, symInfo.AssignedCount, symInfo.ReferencedCount);
            foreach (var use in symInfo.Uses)
            {
                Console.WriteLine("  at Line {0,20}, {1,10}, {2,10}, in {3,11} ", use.LineNumber,
                    use.symbolReadWrite, use.symbolContext,
                    (use.ParentProcedureType == ProcedureType.Proc ? "PROC" : "FN") + use.ParentProcOrFn);
            }
        }
    }
}