using BasAnalysis.CLI;
using BasTools.Core;
using System.Globalization;
#pragma warning disable CA1861, CA1305, CA1304

namespace BasAnalysis.CLI
{
    public class Program
    {
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
                        Utilities.help(arglist[1..]);
                        break;
                    case "LOAD":
                        if (arglist.Length < 2)
                            Utilities.help(new string[] { "LOAD" });
                        else
                            load(arglist[1], BAprogInfo, engine, ref prompt);
                        break;
                    case "CLS":
                    case "CLEAR":
                        Console.Clear();
                        break;
                    case "PREVIEW":
                        Preview(engine); break;
                    case "BLIST":
                        ListProg(engine, arglist[1..], true); break;
                    case "LIST":
                        ListProg(engine, arglist[1..], false); break;
                    case "LISTIF":
                        ListIf(engine, arglist[1..]); break;
                    case "ANALYZE":
                    case "ANALYSE":
                        Analyse(engine, ref analyzed);
                        break;
                    case "LVAR":
                        Listvar(engine, arglist[1..], analyzed);
                        break;
                    case "LVARS":
                        Listvars(engine, arglist[1..], analyzed);
                        break;
                    case "LPROC":
                        ListProc(engine, arglist[1..], analyzed);
                        break;
                    case "LFN":
                        ListFn(engine, arglist[1..], analyzed);
                        break;
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
            HashSet<string> localVars = new();
            HashSet<string> parameters = new();

            // Tracking which procedure or function (inc root) we are "in"
            ProcedureType procedureType = ProcedureType.Root;
            string procedureName = string.Empty;

            foreach (ProgramLine line in engine.CurrentListing.Lines)
            {
                // line level state
                expectingAssignmentTarget = true; // start of line is start of statement...

                List<Token> tokens = BasToolsEngine.WalkTagged(line.TaggedLine).ToList();

                for (int i = 0; i < tokens.Count; i++)
                {
                    SymbolContext context = SymbolContext.TBD; // Global, Local, Parameter, Call, NA, TBD
                    var tok = tokens[i];

                    if (tok.tag == SemanticTags.StatementSep ||
                        (tok.tag == SemanticTags.Keyword && (tok.value is "REPEAT" or "ELSE")))
                    {
                        expectingAssignmentTarget = true;
                        continue;
                    }

                    if (tok.tag == SemanticTags.Keyword && tok.value == "LOCAL")
                    {
                        int j = i + 1;
                        while (j < tokens.Count && tokens[j].tag != SemanticTags.StatementSep)
                        {
                            if (tokens[j].tag == SemanticTags.Variable)
                            {
                                localVars.Add(tokens[j].value);
                                RecordUse(SemanticTags.Variable, tokens[j].value, line.LineNumber,
                                    SymbolReadOrWrite.Assigned, SymbolContext.Local,
                                    procedureName, procedureType);
                            }
                            j++;
                        }
                        i = j; // Skip or they get counted as references
                        continue;
                    }
                    if (tok.tag == SemanticTags.Keyword && tok.value == "FOR") // without this, FOR i=0 TO 4:NEXT would be an assignment without 'use'
                    {
                        Token? nextVar = Utilities.PeekNextNonSpaceToken(tokens, i);
                        if (nextVar != null && nextVar.tag == SemanticTags.Variable) // anything other than variable is illegal as control variable
                        {
                            RecordUse(SemanticTags.Variable,
                                nextVar.value,
                                line.LineNumber,
                                SymbolReadOrWrite.Referenced,   // synthetic reference
                                SymbolContext.Local, procedureName, procedureType
                            );
                        }
                    }

                    if (tok.tag == SemanticTags.Variable)
                    {
                        // Derive Context for variable
                        if (procedureType == ProcedureType.Root)
                        {
                            context = SymbolContext.Global;
                        }
                        else if (parameters.Contains(tok.value))
                        {
                            context = SymbolContext.Parameter;
                        }
                        else if (localVars.Contains(tok.value))
                        {
                            context = SymbolContext.Local;
                        }
                        else
                        {
                            // Variables inside a PROC/FN but not declared LOCAL or parameter are GLOBAL
                            context = SymbolContext.Global;
                        }

                        // Look ahead to see if this is an assignment
                        Token? next = Utilities.PeekNextNonSpaceToken(tokens, i);

                        bool isAssignment = expectingAssignmentTarget &&
                                            next?.tag == SemanticTags.Operator &&
                                            next?.value == "=";

                        RecordUse(tok.tag, tok.value, line.LineNumber,
                            isAssignment ? SymbolReadOrWrite.Assigned : SymbolReadOrWrite.Referenced,
                           context, procedureName, procedureType);

                        expectingAssignmentTarget = false;
                        continue;
                    }

                    if (tok.tag is SemanticTags.ProcName or SemanticTags.FunctionName)
                    {
                        if (line.IsDef)
                        {
                            if (tok.tag == SemanticTags.ProcName)
                                procedureType = ProcedureType.Proc;
                            else
                                procedureType = ProcedureType.Fn;
                            
                            procedureName = tok.value;

                            // We record the DEF as a 'use' to record line number etc
                            RecordUse(tok.tag, tok.value, line.LineNumber, SymbolReadOrWrite.Assigned,
                                SymbolContext.NA, "", procedureType); // here, procedure type = DEFPROC or DEFFN

                            localVars.Clear();
                            parameters.Clear();

                            // Now parse parameters
                            int j = i + 1;
                            bool inParens = false;

                            while (j < tokens.Count)
                            {
                                if (tokens[j].value == "(") inParens = true;
                                else if (tokens[j].value == ")") break;
                                else if (inParens && tokens[j].tag == SemanticTags.Variable)
                                {
                                    parameters.Add(tokens[j].value);
                                    localVars.Add(tokens[j].value); // parameters are also local
                                }
                                j++;
                            }
                            i = j;
                            // Now record a synthetic assignment for each
                            foreach (var p in parameters)
                            {
                                // Synthetic assignment at line of DEF
                                RecordUse(SemanticTags.Variable,
                                    p,
                                    line.LineNumber,
                                    SymbolReadOrWrite.Assigned,
                                    SymbolContext.Parameter,
                                    procedureName,
                                    procedureType
                                );
                            }
                        }
                        else // is a PROC or FN call
                        {
                            RecordUse(tok.tag, tok.value, line.LineNumber,
                            SymbolReadOrWrite.Referenced,
                            SymbolContext.Call, procedureName, procedureType);  // here, procedure name and type refer to the parent proc
                        }
                        continue;
                    }
                    if (tok.tag is SemanticTags.StringLiteral)
                    {
                        RecordUse(tok.tag, tok.value, line.LineNumber,
                            SymbolReadOrWrite.Assigned,
                            SymbolContext.NA, procedureName, procedureType);    // TODO SymbolContext should record whether part of an argument

                        expectingAssignmentTarget = false;
                        continue;
                    }
                    if (tok.tag is SemanticTags.Label)
                    {
                        RecordUse(tok.tag, tok.value, line.LineNumber,
                            SymbolReadOrWrite.Assigned,                         // .label - all assigned. References tracked in variables
                            SymbolContext.TBD, procedureName, procedureType);   // Context should show if local

                        expectingAssignmentTarget = false;
                        continue;
                    }

                    if (tok.tag == SemanticTags.Operator && tok.value == "=")
                    {
                        // After '=', everything is a reference
                        expectingAssignmentTarget = false;
                        continue;
                    }
                }
                // Reset only when outside any DEF block
                if (!line.IsInDef && !line.IsDef)
                {
                    procedureType = ProcedureType.Root;
                    procedureName = "";                    
                }
            }
            Console.Write($"Analysed {Symbols.Count} unique tokens\n");
            analyzed = true;
            return true;
        }
        static void Listvar(BasToolsEngine engine, string[] arglist, bool analyzed)
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
                        VarDetail(arg, symInfo);
                }
            }
        }
        static void Listvars(BasToolsEngine engine, string[] arglist, bool analyzed)
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
            Utilities.PrintByKind(SymbolKind.StaticInt, Symbols);

            Console.WriteLine("\n  Dynamic Variables (may include labels)");

            // Integers
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  Integer Variables");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  {0,-20}{1,10}{2,11}\n", "Variable", "Assigned", "Referenced");
            Utilities.PrintByKind(SymbolKind.IntVar, Symbols);

            // Real variables
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  Real Number Variables");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  {0,-20}{1,10}{2,11}\n", "Variable", "Assigned", "Referenced");
            Utilities.PrintByKind(SymbolKind.RealVar, Symbols);

            // String variables
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  String Variables");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  {0,-20}{1,10}{2,11}\n", "Variable", "Assigned", "Referenced");
            Utilities.PrintByKind(SymbolKind.StringVar, Symbols);

            // PROCs
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  Sub-procedures (PROCs)");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  {0,-20}{1,10}{2,11}\n", "PROC name", "Declared", "Referenced");
            Utilities.PrintByKind(SymbolKind.Proc, Symbols);

            // FNs
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  Functions (FNs)");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  {0,-20}{1,10}{2,11}\n", "FN name", "Declared", "Referenced");
            Utilities.PrintByKind(SymbolKind.Fn, Symbols);

            // Assembler label
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  Assembler labels");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  {0,-20}{1,10}{2,11}\n", "Label", "Assigned", "Referenced");
            Utilities.PrintByKind(SymbolKind.Label, Symbols);

            // Strings
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  Literal strings");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n  {0,-35}{1,6}{2,10}\n", "String", "Count", "Length");
            Utilities.PrintByKind(SymbolKind.LiteralString, Symbols);
        }
        static void ListProc(BasToolsEngine engine, string[] arglist, bool analysed)
        {
            ProcFnDetail(engine, arglist, analysed, "PROC", "LPROC");
        }
        static void ListFn(BasToolsEngine engine, string[] arglist, bool analysed)
        {
            ProcFnDetail(engine, arglist, analysed, "FN", "LFN");
        }
        static void ProcFnDetail(BasToolsEngine engine, string[] arglist, bool analysed, string prefix, string cmd)
        {
            if (engine.CurrentListing == null)
            {
                Console.WriteLine($"{cmd} - No program loaded.");
                return;
            }
            if (!analyzed)
            {
                Console.WriteLine($"{cmd} - Program '{engine.CurrentProgInfo.ProgName}' has not been analysed.");
                return;
            }
            if (arglist.Length == 0)
            {
                Utilities.help(new string[] { cmd });
                return;
            }

            string name = arglist[0];
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(prefix.Length);
            }
            string key = char.ToUpper(prefix[0]).ToString() + prefix[1..].ToLowerInvariant() + ":" + name;
            Console.WriteLine(key);
            if (!Symbols.TryGetValue(key, out var symInfo))
            {
                Console.WriteLine($"{prefix}{name} not found. (Tip: {prefix}s are case sensitive)");
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  {0}: {1} - Declared: {2} - Called :{3} \n", symInfo.Kind, symInfo.Name, symInfo.AssignedCount, symInfo.ReferencedCount);
            Console.ForegroundColor = ConsoleColor.White;

            // List PROC/FN signature
            var defLine = symInfo.Uses.FirstOrDefault(u =>
               u.symbolContext == SymbolContext.NA &&
               u.symbolReadWrite == SymbolReadOrWrite.Assigned);

            if (defLine != null)
            {
                int lineNumber = defLine.LineNumber;
                ListProg(engine, new string[] { lineNumber.ToString() }, false);
            }
            else
            {
                Console.WriteLine($"No DEF found for {prefix}{name}");
            }

            // List procs/fns used
            var called = Symbols.Values.SelectMany(s => s.Uses
                    .Where(u => u.ParentName == name &&
                (u.CalledKind == SymbolKind.Proc || u.CalledKind == SymbolKind.Fn)).ToList());

            if (called.Any())
            {
                Console.WriteLine($"\n Procedures used in {prefix}{name}\n");
                foreach (var use in called)
                {
                    Console.WriteLine("   {0}: {1}{2} at {3}", use.symbolContext, use.CalledKind.ToString().ToUpper(), use.CalledName, use.LineNumber);
                }
            }
            // List vars used
            /*called = Symbols.Values.SelectMany(s => s.Uses
                    .Where(u => u.ParentName == name &&
                (u.CalledKind == SymbolKind.IntVar || u.CalledKind == SymbolKind.RealVar || u.CalledKind == SymbolKind.StringVar || u.CalledKind == SymbolKind.StaticInt))
                    .Distinct().ToList());

            if (called.Any())
            {
                Console.WriteLine($"\n Variables used in {name}\n");
                foreach (var use in called)
                {
                    Console.WriteLine("   {0,10}: {1} {2,10}{3,10} at {4}", use.symbolReadWrite, use.symbolContext, use.CalledKind, use.CalledName, use.LineNumber);
                }
            }*/
            var varsUsed = Symbols.Values
                .SelectMany(s => s.Uses
                    .Where(u => u.ParentName == name &&
                        (u.CalledKind == SymbolKind.IntVar ||
                         u.CalledKind == SymbolKind.RealVar ||
                         u.CalledKind == SymbolKind.StringVar ||
                         u.CalledKind == SymbolKind.StaticInt)))
                .GroupBy(u => u.CalledName)
                .Select(g => new
                {
                    Name = g.Key,
                    Kind = g.First().CalledKind,
                    Context = g.First().symbolContext,   // usually Global or Local
                    Assigned = g.Count(u => u.symbolReadWrite == SymbolReadOrWrite.Assigned),
                    Referenced = g.Count(u => u.symbolReadWrite == SymbolReadOrWrite.Referenced)
                })
                .OrderBy(v => v.Name)
                .ToList();

            Console.WriteLine($"\n Variables used in {prefix}{name}\n");

            foreach (var v in varsUsed)
            {
                Console.WriteLine(
                    "{0,10} {1,12} {2,10}  Assigned: {3,3}  Referenced: {4,3}",
                    v.Context, v.Kind, v.Name, v.Assigned, v.Referenced);
            }


        }
        static void Preview(BasToolsEngine engine)
        {
            if (engine.CurrentListing == null)
            {
                Console.WriteLine("PREVIEW - No program loaded.");
                return;
            }
            List(engine, 0, 0xFEFF, 20, true);
        }
        static void ListProg(BasToolsEngine engine, string[] arglist, bool pretty)
        {
            if (engine.CurrentListing == null)
            {
                Console.WriteLine("LIST - No program loaded.");
                return;
            }
            int fromline = 0;
            int toline = 0xFEFF;

            if (arglist.Length > 0)
            {
                if (!int.TryParse(arglist[0], out fromline)) // If first argument not a number, list DEF
                {
                    ListDef(engine, arglist, pretty);
                    return;
                }
                else
                {
                    toline = fromline; // so LIST nn displays just one line
                }
            }
            if (arglist.Length > 1)
            {
                if (!int.TryParse(arglist[1], out toline)) // If second argument not a number
                    toline = 0xFEFF;                       // default to end
            }
            List(engine, fromline, toline, 0, pretty);
        }
        static void ListIf(BasToolsEngine engine, string[] arglist)
        {
            if (engine.CurrentListing == null)
            {
                Console.WriteLine("LISTIF - No program loaded.");
                return;
            }
            if (arglist.Length < 1)
            {
                Utilities.help(new string[] { "listif" });
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
        static void List(BasToolsEngine engine, int fromline, int toline, int totLineCount, bool pretty)
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
                    //if (!Utilities.printLine(progLine, ref linesprinted)) break;
                    if (pretty)
                    {
                        if (!BasToolsEngine.PrintOneLine(progLine, ref linesprinted)) break;
                    }
                    else
                    {
                        if (!Utilities.printLine(progLine, ref linesprinted)) break;
                    }
                }
            }
        }
        static void ListDef(BasToolsEngine engine, string[] arglist, bool pretty)
        {
            if (engine.CurrentListing == null)
            {
                Console.WriteLine("No program loaded.");
                return;
            }

            for (int i = 0; i < arglist.Length; i++)
            {
                if (!(arglist[i].StartsWith("FN", StringComparison.OrdinalIgnoreCase) || arglist[i].StartsWith("PROC", StringComparison.OrdinalIgnoreCase)))
                {
                    Utilities.help(new string[] { "list" });
                    Console.WriteLine("                i.e. List <FNname | PROCname> [<FNname | PROCname>] ...");
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
                    if (pretty)
                    {
                        if (!BasToolsEngine.PrintOneLine(progLine, ref linesprinted)) break;
                    }
                    else
                    {
                        if (!Utilities.printLine(progLine, ref linesprinted)) break;
                    }

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
                foreach (string arg in arglist)
                {
                    if (string.Equals(name, arg, StringComparison.Ordinal))
                    {
                        listme = true;
                        Console.WriteLine("");
                        // need to print out the DEF line
                        if (pretty)
                            BasToolsEngine.PrintOneLine(progLine, ref linesprinted);
                        else
                            Utilities.printLine(progLine, ref linesprinted);
                        break;
                    }
                }
            }
        }
        static void RecordUse(string tag, string name, int line,
            SymbolReadOrWrite readwrite, SymbolContext context,
            string currentProcName, ProcedureType procedureType) // , SymbolInfo parentSymbolInfo
        {
            //Console.WriteLine("{0,-10}{1,-10}{2,-10},{3,-10}", name, line, context, currentProcName);
            if (tag == SemanticTags.StringLiteral && string.IsNullOrWhiteSpace(name))
                return;

            SymbolKind kind = Utilities.InferKind(tag, name);

            if (!Symbols.TryGetValue(kind + ":" + name, out var sym)) // If first sight, create SymbolInfo with key "<kind>:<name>"
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
                CalledName = name,
                CalledKind = kind,
                LineNumber = line,
                symbolReadWrite = readwrite,
                symbolContext = context,
                ParentName = currentProcName,
                ParentProcedureType = procedureType
            };
            sym.Uses.Add(symbolUse);
        }
        
        private static void VarDetail(string match, SymbolInfo symInfo)
        {
            SymbolKind varKind = Utilities.InferKind("", match);
            if (symInfo.Kind != varKind) return;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  {0}: {1} - Assigned: {2} - Referenced :{3} \n", symInfo.Kind, symInfo.Name, symInfo.AssignedCount, symInfo.ReferencedCount);
            Console.ForegroundColor = ConsoleColor.White;

            foreach (var use in symInfo.Uses)
            {
                Console.WriteLine("  at line {0,5}, {1,10}, {2,10}, in {3}",
                    use.LineNumber,
                    use.symbolReadWrite,
                    use.symbolContext,
                    use.ParentProcedureType + use.ParentName);
            }
        }
    }
}