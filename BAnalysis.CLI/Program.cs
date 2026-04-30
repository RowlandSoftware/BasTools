using BasAnalysis.CLI;
using BasTools.Core;
using System.Globalization;
using System.Text.RegularExpressions;
using Windows.Networking;
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
                if (args.Length > 1)
                {
                    for (int i = 1; i < args.Length; i++)
                    {
                        string arg1 = args[i].ToUpper(CultureInfo.InvariantCulture);
                        if (arg1.StartsWith('/') || arg1.StartsWith('-'))
                            arg1 = arg1.Substring(1);
                        if ("ANALYSE".StartsWith(arg1,StringComparison.OrdinalIgnoreCase) || "ANALYZE".StartsWith(arg1, StringComparison.OrdinalIgnoreCase))
                                Analyse(engine, ref analyzed);
                        if ("PREVIEW".StartsWith(arg1, StringComparison.OrdinalIgnoreCase))
                        Preview(engine);
                    }
                }
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

                if (cmd.Length > 1 && cmd.EndsWith('.'))
                {
                    string abbrev = cmd.Substring(0, cmd.Length - 1);
                    string[] commands = { "HELP", "BLIST", "LIST", "LISTIF", "LOAD", "ANALYZE", "ANALYSE", "CAT", "DIR", "LS",
                        "CLEAR", "CLS", "LVAR", "LVARS", "LFN", "LPROC", "TREE", "PREVIEW", "EXIT", "END", "QUIT" };

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
                    case "DIR":
                    case "LS":
                    case "CAT":
                    case ".":
                        Utilities.Command_DirW();
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
                    case "L.IF":
                    case "LISTIF":
                        ListIf(engine, arglist[1..]);
                        break;
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
                    case "TREE":
                        Tree(engine, arglist[1..], analyzed);
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
        static void Analyse(BasToolsEngine engine, ref bool analyzed)
        {
            if (!Utilities.checkLoaded("ANALYSE", engine)) return;

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

                    if (tok.tag == SemanticTags.Keyword && tok.value == "LOCAL") // variables in the LOCAL list count as assignments (initialised to 0 or "")
                    {
                        int j = i + 1;
                        while (j < tokens.Count && tokens[j].tag != SemanticTags.StatementSep)
                        {
                            if (tokens[j].tag == SemanticTags.Variable)
                            {
                                localVars.Add(tokens[j].value);                 // build list of LOCAL vars

                                RecordUse(SemanticTags.Variable, tokens[j].value, line.LineNumber,
                                    SymbolReadOrWrite.Assigned, SymbolContext.Local,
                                    procedureName, procedureType);              // and record an assignment
                            }
                            j++;
                        }
                        i = j; // Skip past or they get counted as references too
                        continue;
                    }

                    if (tok.tag == SemanticTags.IndentingKeyword && tok.value == "FOR") // without this, FOR i=0 TO 4:NEXT would be an assignment without 'use'
                    {
                        Token? nextVar = Utilities.PeekNextNonSpaceToken(tokens, i);
                        if (nextVar != null && nextVar.tag == SemanticTags.Variable) // anything other than variable is illegal as control variable
                        {
                            RecordUse(SemanticTags.Variable,
                                nextVar.value,
                                line.LineNumber,
                                SymbolReadOrWrite.Referenced,                       // synthetic reference
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
                        // look at LOCAL and parameters lists (W/0 .)
                        SymbolContext labcontext = SymbolContext.Assembler;
                        if (localVars.Contains(tok.value[1..]))
                            labcontext = SymbolContext.Local;
                        if (parameters.Contains(tok.value[1..]))
                            labcontext = SymbolContext.Parameter;

                        RecordUse(tok.tag, tok.value, line.LineNumber,
                        SymbolReadOrWrite.Assigned,                     // .label - all assigned. References tracked in variables
                        labcontext, procedureName, procedureType);      // Context should show if local or parameter, otherwise Assembler

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
            return;
        }
        static void Listvar(BasToolsEngine engine, string[] arglist, bool analyzed)

        {
            if (!Utilities.checkLoaded("LVAR", engine)) return;
            if (!Utilities.checkAnalysed("LVAR", engine.CurrentProgInfo.ProgName, analyzed)) return;

            // Find variable
            for (int j = 0; j < arglist.Length; j++)
            {
                string arg = arglist[j];

                var varUsage = Symbols.Values.Where(s => s.Name == arg &&
                       (s.Kind == SymbolKind.IntVar ||
                        s.Kind == SymbolKind.RealVar ||
                        s.Kind == SymbolKind.StringVar ||
                        s.Kind == SymbolKind.Label ||
                        s.Kind == SymbolKind.StaticInt))
                .SelectMany(s => s.Uses)
                .GroupBy(u => new { u.ParentName, u.ParentProcedureType, u.symbolContext })
                .Select(g => new
                {
                    ProcName = g.Key.ParentName,
                    ProcType = g.Key.ParentProcedureType,
                    Context = g.Key.symbolContext,
                    Assigned = g.Count(u => u.symbolReadWrite == SymbolReadOrWrite.Assigned),
                    Referenced = g.Count(u => u.symbolReadWrite == SymbolReadOrWrite.Referenced),
                    LineNumbers = g.Select(u => u.LineNumber)
                                   .Distinct()
                                   .OrderBy(n => n)
                                   .ToList()
                })
                .OrderBy(x => x.ProcName)
                .ToList();

                if (varUsage.Count == 0)
                {
                    Console.WriteLine($"No such variable '{arg}'.");
                    return;
                }
                else
                {
                    SymbolKind varKind = Utilities.InferKind(SemanticTags.Variable, arg); // tag here only used if no %, $ or leading .
                    //SymbolInfo symInfo = Symbols[varKind + ":" + arg];
                    if (!Symbols.TryGetValue(varKind + ":" + arg, out SymbolInfo symInfo))
                    {
                        Console.WriteLine($"No such variable '{arg}'.");
                        return;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("\n  {0}: {1} - Assigned: {2} - Referenced :{3} \n", symInfo.Kind, symInfo.Name, symInfo.AssignedCount, symInfo.ReferencedCount);
                        Console.ForegroundColor = ConsoleColor.White;
                    }

                    foreach (var u in varUsage)
                    {
                        string prefix =
                            u.ProcType == ProcedureType.Proc ? "PROC" :
                            u.ProcType == ProcedureType.Fn ? "FN" :
                            "";

                        Console.WriteLine($"  in {prefix}{u.ProcName}:");
                        if (u.Assigned > 0 && u.Referenced == 0 && symInfo.Kind != SymbolKind.Label)
                            Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("     {0,9}, Assigned: {1,3}  Referenced: {2,3}  at {3}", u.Context, u.Assigned, u.Referenced, string.Join(", ", u.LineNumbers));
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    if (symInfo.Kind == SymbolKind.Label)
                        Listvar(engine, new string[] { symInfo.Name[1..] }, true); // also information for the variable w/o dot
                }
            }
        }
        static void Listvars(BasToolsEngine engine, string[] arglist, bool analyzed)
        {
            if (!Utilities.checkLoaded("LVARS", engine)) return;
            if (!Utilities.checkAnalysed("LVARS", engine.CurrentProgInfo.ProgName, analyzed)) return;

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
            if (!Utilities.checkLoaded($"{cmd}", engine)) return;
            if (!Utilities.checkAnalysed(cmd, engine.CurrentProgInfo.ProgName, analyzed)) return;
            if (arglist.Length == 0)
            {
                Utilities.help(new string[] { cmd });
                return;
            }

            // Normalise argument - PROCwrite or write -> write
            string name = arglist[0];
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(prefix.Length);
            }
            // Find the SymbolInfo
            string key = Utilities.InitCap(prefix) + ":" + name;
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

            // List callers
            var usedBy = Symbols.Values.SelectMany(s => s.Uses
                .Where(u =>
                    u.CalledName == name &&   // ← this PROC/FN is being called
                    u.symbolContext == SymbolContext.Call &&
                    (u.CalledKind == SymbolKind.Proc ||
                     u.CalledKind == SymbolKind.Fn)))
            .GroupBy(u => new { u.ParentName, u.ParentProcedureType })  // group by caller
            .Select(g => new
            {
                CallerName = g.Key.ParentName,
                CallerType = g.Key.ParentProcedureType,
                LineNumbers = g.Select(u => u.LineNumber)
                               .Distinct()
                               .OrderBy(n => n)
                               .ToList()
            })
            .OrderBy(x => x.CallerName)
            .ToList();

            if (usedBy.Count > 0)
            {
                Console.WriteLine($"\n {prefix}{name} is used:");

                foreach (var u in usedBy)
                {
                    Console.WriteLine($"   in {(u.CallerName == "" ? "Root" : u.CallerType.ToString().ToUpper() + u.CallerName)} at {string.Join(", ", u.LineNumbers)}");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n {prefix}{name} is not called anywhere.\n");
                Console.ForegroundColor = ConsoleColor.White;
            }

            // List procs/fns used
            var procsUsed = Symbols.Values.SelectMany(s => s.Uses
                .Where(u => u.ParentName == name &&
                       (u.CalledKind == SymbolKind.Proc ||
                        u.CalledKind == SymbolKind.Fn)))
            .GroupBy(u => u.CalledName)
            .Select(g => new
            {
                Name = g.Key,
                Kind = g.First().CalledKind,
                Context = g.First().symbolContext,
                LineNumbers = g.Select(u => u.LineNumber)
                               .Distinct()
                               .OrderBy(n => n)
                               .ToList()
            })
            .OrderBy(v => v.Name)
            .ToList();

            if (procsUsed.Count > 0)
            {
                Console.WriteLine($"\n Sub-procedures used in {prefix}{name}\n");

                foreach (var p in procsUsed)
                {
                    Console.WriteLine(
                        "   {1}{2} at {3}",
                        p.Context,
                        p.Kind.ToString().ToUpper(),
                        p.Name,
                        string.Join(", ", p.LineNumbers));
                }
            }

            // List vars used
            var varsUsed = Symbols.Values.SelectMany(s => s.Uses
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
                Context = g.First().symbolContext,
                Assigned = g.Count(u => u.symbolReadWrite == SymbolReadOrWrite.Assigned),
                Referenced = g.Count(u => u.symbolReadWrite == SymbolReadOrWrite.Referenced),
                // collect line numbers
                LineNumbers = g.Select(u => u.LineNumber).Distinct().OrderBy(n => n).ToList()
            })
            .OrderBy(v => v.Name)
            .ToList();

            if (varsUsed.Count > 0)
            {
                Console.WriteLine($"\n Variables used in {prefix}{name}\n");

                foreach (var v in varsUsed)
                {
                    if (v.Assigned >0 && v.Referenced == 0) Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(
                        "  {0,9} {1,12} {2,10}  Assigned: {3,3}  Referenced: {4,3}  at {5}",
                        v.Context, v.Kind, v.Name, v.Assigned, v.Referenced,
                        string.Join(", ", v.LineNumbers));
                    Console.ForegroundColor = ConsoleColor.White;
                }
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
            if (!Utilities.checkLoaded("LIST", engine)) return;
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
            if (!Utilities.checkLoaded("LISTIF", engine)) return;
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
        static void Tree(BasToolsEngine engine, string[] arglist, bool analyzed)
        {
            if (!Utilities.checkLoaded("TREE", engine)) return;
            if (!Utilities.checkAnalysed("TREE", engine.CurrentProgInfo.ProgName, analyzed)) return;

            Command_Tree(Symbols, arglist);
        }

        // =====================================
        // 1. Build nodes (PROC/FN only)
        // =====================================

        static Dictionary<string, CallNode> BuildNodes(IReadOnlyDictionary<string, SymbolInfo> Symbols)
        {
            var nodes = new Dictionary<string, CallNode>(StringComparer.Ordinal);

            foreach (var symInfo in Symbols.Values)
            {
                if (symInfo.Kind == SymbolKind.Proc || symInfo.Kind == SymbolKind.Fn)
                {
                    string fullName = Utilities.FullName(symInfo.Kind, symInfo.Name);
                    Utilities.GetOrAdd(nodes, fullName);
                }
            }

            // Ensure ROOT exists
            Utilities.GetOrAdd(nodes, "ROOT");

            return nodes;
        }

        // =====================================
        // 2. Build edges (parent → child, with line numbers)
        // =====================================

        static void BuildCallEdges(
            IReadOnlyDictionary<string, SymbolInfo> Symbols,
            Dictionary<string, CallNode> nodes)
        {
            foreach (var symInfo in Symbols.Values)
            {
                foreach (var use in symInfo.Uses)
                {
                    if (use.CalledKind != SymbolKind.Proc &&
                        use.CalledKind != SymbolKind.Fn)
                        continue;

                    string parentName = Utilities.FullName(use.ParentProcedureType, use.ParentName);
                    string childName = Utilities.FullName(use.CalledKind, use.CalledName);

                    var parentNode = Utilities.GetOrAdd(nodes, parentName);
                    var childNode = Utilities.GetOrAdd(nodes, childName);

                    // Add edge with call-site line number
                    parentNode.Children.Add(new CallEdge(childNode, use.LineNumber));
                }
            }
        }

        // =====================================
        // 3. Compute MaxDepth (node-based)
        // =====================================

        static int ComputeDepth(CallNode node, HashSet<CallNode> visiting)
        {
            if (visiting.Contains(node))
                return node.MaxDepth; // recursion

            if (node.Children.Count == 0)
                return node.MaxDepth = Math.Max(node.MaxDepth, 1);

            visiting.Add(node);

            int depth = 1 + node.Children
                .Select(e => ComputeDepth(e.Child, visiting))
                .DefaultIfEmpty(0)
                .Max();

            visiting.Remove(node);

            return node.MaxDepth = Math.Max(node.MaxDepth, depth);
        }

        // =====================================
        // 4. Print tree (edge-ordered, node-unique)
        // =====================================

        static void PrintTree(CallNode node, string indent, bool last, HashSet<CallNode> printed)
        {
            string prefix = last ? "└─ " : "├─ ";

            if (printed.Contains(node))
            {
                if (node.Children.Count == 0)
                {
                    // Leaf node: repeat without annotation
                    Console.WriteLine(indent + prefix + node.Name + " *");
                }
                else
                {
                    // Non-leaf: show reference
                    Console.WriteLine(indent + prefix + node.Name + "  (see above)");
                }
                return;
            }
            printed.Add(node);
            Console.WriteLine(indent + prefix + node.Name);

            var children = node.Children
                .OrderBy(e => e.LineNumber)           // program order
                .ThenByDescending(e => e.Child.MaxDepth)
                .ToList();

            for (int i = 0; i < children.Count; i++)
            {
                bool isLast = (i == children.Count - 1);
                PrintTree(children[i].Child,
                          indent + (last ? "   " : "│  "),
                          isLast,
                          printed);
            }
        }

        // =====================================
        // 5. Entry point: tree command
        // =====================================

        static void Command_Tree(IReadOnlyDictionary<string, SymbolInfo> Symbols, string[] arglist)
        {
            // 1. Build nodes
            var nodes = BuildNodes(Symbols);

            // 2. Build edges
            BuildCallEdges(Symbols, nodes);

            // 3. Resolve root
            string rootName;

            if (arglist.Length > 0)
            {
                rootName = arglist[0].Trim();
                if (rootName == "$" || rootName.Equals("root", StringComparison.OrdinalIgnoreCase))
                    rootName = "ROOT";

                if (!rootName.StartsWith("PROC", StringComparison.OrdinalIgnoreCase) &&
                    !rootName.StartsWith("FN", StringComparison.OrdinalIgnoreCase) &&
                    !rootName.Equals("ROOT", StringComparison.OrdinalIgnoreCase))
                {
                    if (nodes.ContainsKey("PROC" + rootName))
                        rootName = "PROC" + rootName;
                    else if (nodes.ContainsKey("FN" + rootName))
                        rootName = "FN" + rootName;
                }
            }
            else
            {
                rootName = "ROOT";
            }

            if (!nodes.TryGetValue(rootName, out var rootNode))
            {
                Console.WriteLine($"Unknown procedure/function: {rootName}");
                return;
            }

            // 4. Compute depths
            ComputeDepth(rootNode, new HashSet<CallNode>());

            // 5. Print
            var printed = new HashSet<CallNode>();

            // Special-case root: print without prefix
            if (rootNode.Name == "ROOT")
                Console.WriteLine('$');
            else
                Console.WriteLine(rootNode.Name);

            var children = rootNode.Children
                .OrderBy(e => e.LineNumber)
                .ThenByDescending(e => e.Child.MaxDepth)
                .ToList();

            for (int i = 0; i < children.Count; i++)
            {
                bool isLast = (i == children.Count - 1);
                PrintTree(children[i].Child,
                          "",   // indent starts empty
                          isLast,
                          printed);
            }
        }
        static void RecordUse(string tag, string name, int line,
            SymbolReadOrWrite readwrite, SymbolContext context,
            string currentProcName, ProcedureType procedureType) // , SymbolInfo parentSymbolInfo
        {
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
    }
}