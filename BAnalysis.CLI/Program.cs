using BAnalysis.CLI;
using BasTools.Core;

namespace BasList.CLI
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

            public int AssignedCount { get; set; } = 0;
            public int ReferencedCount { get; set; } = 0;

            public List<SymbolUse> Uses { get; } = new();   // line numbers, contexts, parent
        }

        static Dictionary<string, SymbolInfo> Symbols = new();
        static bool analyzed = false;

        static void Main(string[] args)
        {
            string cmd = string.Empty;
            string prompt = "BasAnalysis >";

            BasToolsEngine engine = new BasToolsEngine();
            //bool flgZ80 = false;
            ProgInfo CurrentProgInfo = new(); // (flgZ80, switches.BasicV, filename);
            FormattingOptions formatOptions = new(); // switches.copyToFormatOptions();

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
                load(args[0], CurrentProgInfo, engine, ref prompt);
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
                cmd = arglist[0].ToUpper();

                if (cmd.EndsWith('.'))
                {
                    string abbrev = cmd.Substring(0, cmd.Length - 1);
                    string[] commands = { "HELP", "LIST", "LOAD", "ANALYZE", "ANALYSE", "LVAR", "LVARS", "LFN", "LPROC", "TREE", "PREVIEW", "EXIT", "END", "QUIT" };
                    // "LISTIF", "LISTIFX", "BLIST"
                    foreach (string match in commands)
                    {
                        if (match.StartsWith(abbrev))
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
                        load(arglist[1], CurrentProgInfo, engine, ref prompt);
                        break;
                    case "PREVIEW":
                        Preview(engine); break;
                    case "LIST":
                        ListProg(engine, arglist); break;
                    case "ANALYZE":
                    case "ANALYSE":
                        Analyse(engine, ref analyzed);
                        break;
                    case "LVAR":
                        Listvars(arglist, engine, analyzed);
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
        static bool load(string filename, ProgInfo CurrentProgInfo, BasToolsEngine engine, ref string prompt)
        {
            try
            {
                CurrentProgInfo = new();
                FormattingOptions formatOptions = new FormattingOptions
                {
                    Align = true,
                    AssemblerColumns = true,
                    FlgAddNums = true,
                    FlgIndent = true
                };

                engine.loadAndDetokenise(filename, formatOptions, CurrentProgInfo);
                //Listing formattedListing = engine.loadAndFormatFile(filename, formatOptions, CurrentProgInfo);

                prompt = "BasAnalysis " + Path.GetFileName(filename) + " >";

                Symbols.Clear();
                analyzed = false;

                Console.WriteLine($"Program loaded. {CurrentProgInfo.NumberOfLines} lines, {CurrentProgInfo.LengthInBytes} bytes " +
                $"(&{CurrentProgInfo.LengthInBytes:X4}), {CurrentProgInfo.LengthInBytes / 1024.0:F2} KB" +
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
                Console.WriteLine("No program loaded.");
                return false;
            }

            bool lhs;
            ProcedureType procedureType = ProcedureType.Root;
            string procedureName = string.Empty;

            foreach (ProgramLine line in engine.CurrentListing.Lines)
            {
                // line level state
                lhs = true;
                //Console.Write($"{line.LineNumber} ");
                foreach (Token tok in BasToolsEngine.WalkTagged(line.TaggedLine))
                {
                    if (tok.tag != null && tok.value != null)
                    {
                        if (tok.tag == SemanticTags.Variable || tok.tag == SemanticTags.StaticInteger ||
                        tok.tag == SemanticTags.Label || tok.tag == SemanticTags.FunctionName ||
                        tok.tag == SemanticTags.ProcName || tok.tag == SemanticTags.StringLiteral)
                        {
                            RecordUse(tok.tag, tok.value, line.LineNumber,
                            lhs ? SymbolReadOrWrite.Assigned : SymbolReadOrWrite.Referenced,
                            SymbolContext.TBD, procedureName, procedureType);
                        }
                        if (tok.tag == SemanticTags.Operator && tok.value == "=")
                        {
                            lhs = false;
                        }
                        if (tok.tag == SemanticTags.StatementSep && tok.value == ":")
                        {
                            lhs = true;
                        }
                    }
                }
            }
            Console.Write($"Analysed {Symbols.Count} unique tokens\n");
            analyzed = true;
            return true;
        }
        static void Listvar(string[] arglist, BasToolsEngine engine, bool analyzed)
        {
            if (engine.CurrentListing == null)
            {
                Console.WriteLine("No program loaded.");
                return;
            }
            if (!analyzed)
            {
                Console.WriteLine("Program has not been analysed.");
                return;
            }

            // Find variable
            bool first = true;
            foreach (string arg in arglist)
            {
                if (first)
                {
                    first = false;
                    continue;
                }

            }
            Console.WriteLine("{0,-20}{1,-10}{2,10}{3,11}", "Variable", "Kind", "Assigned", "Referenced");

            foreach (SymbolInfo symInfo in Symbols.Values)
            {
                Console.WriteLine("{0,-20}{1,-10}{2,10}{3,11}", symInfo.Name, symInfo.Kind, symInfo.AssignedCount, symInfo.ReferencedCount);
            }
        }
        static void Listvars(string[] arglist, BasToolsEngine engine, bool analyzed)
        {
            if (engine.CurrentListing == null)
            {
                Console.WriteLine("No program loaded.");
                return;
            }
            if (!analyzed)
            {
                Console.WriteLine("Program has not been analysed.");
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
            List(engine, 0, 0xFEFF, 20);
        }
        static void ListProg(BasToolsEngine engine, string[] arglist)
        {
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
                int.TryParse(arglist[2], out toline);
            }
            List(engine, fromline, toline, 0);
        }
        static void List(BasToolsEngine engine, int fromline, int toline, int totLineCount)
        {
            if (engine.CurrentListing == null)
            {
                Console.WriteLine("No program loaded.");
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
                    if (string.Equals(name, arg, StringComparison.InvariantCultureIgnoreCase))
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
    }
}