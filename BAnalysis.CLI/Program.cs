using BAnalysis.CLI;
using BasTools.Core;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml.Linq;
using Windows.Media;
using static System.Windows.Forms.LinkLabel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

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
            Array,
            Proc,
            Fn,
            LocalVar,
            Parameter
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
            public string Context { get; init; } = "";   // "assigned", "read", "call", "parameter", etc.
            public string? ParentProcOrFn { get; set; }     // null = global
        }

        class SymbolInfo
        {
            public string Name { get; init; } = "";
            public SymbolKind Kind { get; init; }   // IntVar, FPVar, StringVar, Array, PROC, FN, LocalVar, etc.

            public int AssignedCount { get; set; }
            public int ReferencedCount { get; set; }

            public List<SymbolUse> Uses { get; } = new();   // line numbers, contexts, parent
        }

        static Dictionary<string, SymbolInfo> Symbols = new();


        static void Main(string[] args)
        {
            bool loaded = false;
            bool analyzed = false;
            string cmd = string.Empty;
            string prompt = "BasAnalysis >";

            BasToolsEngine engine = new BasToolsEngine();
            bool flgZ80 = false;
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
                load(args[0], CurrentProgInfo, engine, ref loaded, ref prompt);
            }
            //
            // Main Loop
            //
            while (cmd != "QUIT" && cmd != "EXIT")
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();
                if (input == null) continue;

                string[] arglist = Utilities.SplitArgList(input);
                cmd = arglist[0].ToUpper();

                if (cmd.EndsWith('.'))
                {
                    string abbrev = cmd.Substring(0, cmd.Length - 1);
                    string[] commands = { "HELP", "LOAD", "ANALYZE", "ANALYSE", "LIST", "LVAR", "LVARS", "LFN", "LPROC", "TREE", "PREVIEW", "EXIT", "QUIT" };
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
                    case "HELP": Utilities.help(arglist); break;
                    case "LOAD":
                        load(arglist[1], CurrentProgInfo, engine, ref loaded, ref prompt);
                        break;
                    case "PREVIEW":
                        Preview(engine); break;
                    case "LIST":
                        ListProg(engine, arglist); break;
                    case "ANALYZE":
                    case "ANALYSE":
                        Analyse(engine);
                        break;
                    case "LVARS":
                        Listvars(arglist)
                            ; break;
                    case "QUIT":
                    case "EXIT": break;
                    default:
                        Console.WriteLine($"'{cmd}' not recognised");
                        break;
                }
            }
        }
        static bool load(string filename, ProgInfo CurrentProgInfo, BasToolsEngine engine, ref bool loaded, ref string prompt)
        {
            try
            {
                CurrentProgInfo = new();
                engine.loadAndDetokenise(filename, CurrentProgInfo);

                loaded = true;
                prompt = "BasAnalysis " + Path.GetFileName(filename) + " >";

                Symbols.Clear();

                Console.WriteLine($"Program loaded. {CurrentProgInfo.NumberOfLines} lines, {CurrentProgInfo.LengthInBytes} bytes " +
                $"(&{CurrentProgInfo.LengthInBytes:X4}), {CurrentProgInfo.LengthInBytes / 1024.0:F2} KB" +
                $"\n\nEnter 'preview' to see first 20 lines, or enter 'analyse'");
                
                return true;
            }
            catch (BasToolsException e)
            {
                Console.WriteLine($"{e.Message}");
            }
            return false;
        }
        static bool Analyse(BasToolsEngine engine)
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
                Console.Write($"{line.LineNumber} ");
                foreach (Token tok in BasToolsEngine.WalkTagged(line.TaggedLine))
                {
                    if (tok.tag != null)
                    {
                        if (tok.tag == SemanticTags.Variable && tok.value != null)
                        {
                            Console.WriteLine($"{tok.tag} {tok.value}");
                            RecordUse(tok.value, line.LineNumber, lhs ? "assigned" : "referenced", procedureName);
                        }
                        if (tok.tag == SemanticTags.Operator && tok.value == "=")
                        {
                            lhs = false;
                        }
                        if (tok.tag == SemanticTags.StatementSep && tok.value == ":")
                        {
                            lhs = true;
                        }
                        if (tok.tag == SemanticTags.FunctionName && tok.value != null)
                        {
                            procedureName = tok.value;
                            procedureType = ProcedureType.Fn;
                            //SymbolInfo sym = new SymbolInfo { Name = tok.value, Kind = SymbolKind.Fn};
                            //sym.Uses.Add(sym);
                        }
                    }
                }
            }
            return true;
        }
        static void Listvars(string[] arglist)
        {
            Console.WriteLine("{0,-20}{1,-10}{2,10}{3,11}", "Variable", "Kind", "Assigned", "Referenced");

            foreach (SymbolInfo symInfo in Symbols.Values)
            {
                Console.WriteLine("{0,-20}{1,-10}{2,10}{3,11}", symInfo.Name, symInfo.Kind, symInfo.AssignedCount, symInfo.ReferencedCount);
            }
        }
        static void Preview(BasToolsEngine engine)
        {
            List(engine, 0, 0xFFFF, 20);
        }
        static void ListProg(BasToolsEngine engine, string[] arglist)
        {
            int fromline = 0;
            int toline = 0xFFFF;

            if (arglist.Length > 1)
            {
                int.TryParse (arglist[1], out fromline);
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

            for (int i = 0 ; i < 0xFEFF && (totLineCount == 0 ? true : ++linecount <= totLineCount); i++)
            {
                if (i == engine.CurrentListing.Lines.Count)
                    return;

                ProgramLine progLine = engine.CurrentListing.Lines[i];

                if (progLine.LineNumber > toline) return;

                if (progLine.LineNumber >= fromline)
                {                   
                    string line = progLine.PlainDetokenisedLine;

                    string printedLine = progLine.LineNumber.ToString().PadLeft(5) + ' ' + line;

                    Console.WriteLine(printedLine);

                    int windowWidth = Console.WindowWidth;
                    int rows = (printedLine.Length + (windowWidth - 1)) / windowWidth;

                    linesprinted += rows;

                    // Deal with pausing
                    switch (Utilities.CheckForPause(ref linesprinted))
                    {
                        case ConsoleKey.Spacebar: linesprinted = 0; break;
                        case ConsoleKey.Enter: linesprinted--; break;
                        case ConsoleKey.Escape: return;
                    }
                }
            }
        }
        static void RecordUse(string name, int line, string context, string currentProcName)
        {
            Console.WriteLine("{0,-10}{1,-10}{2,-10},{3,-10}", name, line, context, currentProcName);
            if (!Symbols.TryGetValue(name, out var sym))
            {
                sym = new SymbolInfo { Name = name, Kind = InferKind(name) };
                Symbols[name] = sym;
            }

            if (context == "assigned")
                sym.AssignedCount++;
            else
                sym.ReferencedCount++;

            SymbolUse symbolUse = new SymbolUse { LineNumber = line, Context = context };
            sym.Uses.Add(symbolUse);

            symbolUse.ParentProcOrFn ??= currentProcName;
        }
        private static SymbolKind InferKind(string name)
        {
            if (name.EndsWith('%') && name.Length == 2 && (char.IsAsciiLetterUpper(name[^1]) || name[^1] == '@'))
                return SymbolKind.StaticInt;
            if (name.EndsWith('%'))
                return SymbolKind.IntVar;
            if (name.EndsWith('$'))
                return SymbolKind.StringVar;
            return SymbolKind.RealVar;
        }
    }
}