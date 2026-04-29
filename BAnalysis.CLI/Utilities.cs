using BasTools.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
#pragma warning disable CA1861, CA1305

namespace BasAnalysis.CLI
{
    internal static class Utilities
    {
        public static SymbolKind InferKind(string tag, string name)
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
            return SymbolKind.Unknown;
            // TODO Arrays?
        }
        public static void PrintByKind(SymbolKind kind, Dictionary<string, SymbolInfo> Symbols)
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
                else if (kind == SymbolKind.Label)
                {
                    if (symInfo.AssignedCount > 1) Console.ForegroundColor = ConsoleColor.Red;       // assigned > once

                    // find corresponding variable (without leading .)
                    int refCount = 0; // symInfo.ReferencedCount;
                    string name = symInfo.Name.Substring(1);
                    SymbolKind refKind = InferKind(SemanticTags.Variable, name);

                    SymbolInfo refVar;
                    if (refKind != SymbolKind.Unknown)
                    {
                        refVar = Symbols[refKind + ":" + name];
                        //Console.WriteLine($"{refVar.Name} - {refVar.ReferencedCount} - ");
                        refCount = refVar.ReferencedCount;
                    }

                    Console.WriteLine("  {0,-20}{1,10}{2,11} ", symInfo.Name, symInfo.AssignedCount, refCount);
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
        public static Token? PeekNextNonSpaceToken(List<Token> tokens, int index)
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
        public static string[] SplitArgList(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Array.Empty<string>();
            }
            List<string> parts = new();
            StringBuilder current = new();
            bool inQuotes = false;
            char quoteChar = '\0';

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                // Start or end of a quoted section
                if ((c == '"' || c == '\''))
                {
                    if (!inQuotes)
                    {
                        inQuotes = true;
                        quoteChar = c;
                    }
                    else if (c == quoteChar)
                    {
                        inQuotes = false;
                    }

                    current.Append(c);
                    continue;
                }

                // Space outside quotes ? split
                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        parts.Add(current.ToString());
                        current.Clear();
                    }
                    continue;
                }

                // Normal character
                current.Append(c);
            }

            // Add final token
            if (current.Length > 0)
                parts.Add(current.ToString());

            return parts.ToArray();
        }

        public static void banner()
        {
            string vs = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion ?? "1.1.0"; // ?? = null coalescing operator. //requires ref to System.Windows.Forms

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nBasAnalysis vs {vs} (C) Andrew Rowland 2022-26");
            Console.WriteLine("Detailed analysis of a BBC BASIC program\n");
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void help(string[] args)
        {
            if (args.Length == 0)
            {
                banner();
                Console.WriteLine("    BasList <filename> [/analyse | /analyze] [/preview]");
                Console.WriteLine("\n    COMMANDS\n");
                Console.WriteLine("    {0,-10}{1,-10}{2,-10}{3,-10}{4,-10}{5,-10}", "help", "load", "analyze", "preview", "list", "blist");
                Console.WriteLine("    {0,-10}{1,-10}{2,-10}{3,-10}{4,-10}{5,-10}", "lvar", "lvars", "lfn", "lproc", "tree", "x");
                Console.WriteLine("    {0,-10}{1,-10}{2,-10}{3,-10}{4,-10}{5,-10}", "cls", "clear", "cat", "dir", "exit", "quit");
                Console.WriteLine("\nEnter help <command> for further help\n");
            }
            else
            {
                Console.WriteLine("\n< > - arguments; [ ] - optional; | - or; { } - list of one or more\n");
                switch (args[0].ToLower(CultureInfo.InvariantCulture))
                {
                    case "load":
                        Console.WriteLine("load <file spec> - If file spec contains spaces, enclose in double quotes");
                        break;
                    case "blist":
                        Console.WriteLine("blist         - As List with syntax colouring");
                        help(new string[] { "list" });
                        break;
                    case "list":
                        Console.WriteLine("list          - Display entire program");
                        Console.WriteLine("list nn       - Display program line");
                        Console.WriteLine("list nn nn    - Display program lines (from to)");
                        Console.WriteLine("list {<name>} - Display PROC or FN (list)");
                        break;
                    case "preview":
                        Console.WriteLine("preview     - Display first 20 lines of program");
                        break;
                    case "analyse":
                    case "analyze":
                        Console.WriteLine("analyze     - (or analyse) Use after 'load' and before other options");
                        break;
                    case "lvars":
                        Console.WriteLine("lvars       - Display analysis of variables, procedures and strings");
                        break;
                    case "lvar":
                        Console.WriteLine("lvar <variable>   - Display detailed analysis of named variable");
                        break;
                    case "lfn":
                        Console.WriteLine("lfn <FN name>     - Display detailed analysis of named function");
                        break;
                    case "lproc":
                        Console.WriteLine("lproc <PROC name> - Display detailed analysis of named procedure");
                        break;
                    case "listif":
                        Console.WriteLine("listif {<text>}   - Display lines that contain <text> (list)");
                        break;
                    case "cls":
                    case "clear":
                        Console.WriteLine("cls | clear - Clear screen");
                        break;
                    case "cat":
                    case "dir":
                    case ".":
                        Console.WriteLine("cat | dir - Catalogue current directory");
                        break;
                    case "exit":
                    case "quit":
                    case "x":
                    case "end":
                        Console.WriteLine($"{args[0]} - Leave BasAnalysis");
                        break;
                    default:
                        Console.WriteLine($"'{args[0]}' not recognised");
                        break;
                }
            }
        }
        /******** Tree Helpers **********/
        public static CallNode GetOrAdd(Dictionary<string, CallNode> dict, string name)
        {
            if (!dict.TryGetValue(name, out var node))
            {
                node = new CallNode(name);
                dict[name] = node;
            }
            return node;
        }
        public static string FullName(ProcedureType? procType, string name)
        {
            return procType switch
            {
                ProcedureType.Proc => "PROC" + name,
                ProcedureType.Fn => "FN" + name,
                ProcedureType.Root => "ROOT",
                _ => name
            };
        }
        public static string FullName(SymbolKind? kind, string name)
        {
            return kind switch
            {
                SymbolKind.Proc => "PROC" + name,
                SymbolKind.Fn => "FN" + name,
                _ => name
            };
        }
        // Print line and check for pause
        // Returns: true - continue, false - stop listing
        internal static bool printLine(ProgramLine progLine, ref int linesprinted)
        {
            string line = progLine.FormattedPlain;
            string printedLine = progLine.LineNumber.ToString().PadLeft(5) + ' ' + new string(' ', progLine.IndentLevel * 2) + line;
            Console.WriteLine(printedLine);

            int windowWidth = Console.WindowWidth;
            int rows = (printedLine.Length + (windowWidth - 1)) / windowWidth;

            linesprinted += rows;

            // Deal with pausing
            switch (Utilities.CheckForPause(ref linesprinted))
            {
                case ConsoleKey.Spacebar: linesprinted = 0; break;
                case ConsoleKey.Enter: linesprinted--; break;
                case ConsoleKey.Escape: return false;
            }
            return true;
        }        
        public static ConsoleKey CheckForPause(ref int linesprinted)
        {
            if (linesprinted == Console.WindowHeight - 4)
            {
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
        public static string InitCap(string s)
        {
            return char.ToUpper(s[0],CultureInfo.InvariantCulture).ToString() + s[1..].ToLowerInvariant();
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
        public static void Command_DirW()
        {
            string cwd = Directory.GetCurrentDirectory();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(" Directory of " + cwd);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();

            var entries = Directory.GetFileSystemEntries(cwd)
                .Select(path => Path.GetFileName(path))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (entries.Count == 0)
            {
                Console.WriteLine(" <empty>");
                return;
            }

            // Choose column width
            int colWidth = 20; // adjust if you like
            int cols = Math.Max(1, Console.WindowWidth / colWidth);

            int index = 0;
            while (index < entries.Count)
            {
                for (int c = 0; c < cols && index < entries.Count; c++, index++)
                {
                    string name = entries[index];

                    // Mark directories
                    if (Directory.Exists(Path.Combine(cwd, name)))
                        name += "/";

                    Console.Write(name.PadRight(colWidth));
                }
                Console.WriteLine();
            }

            Console.WriteLine();
        }

    }
}